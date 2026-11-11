using System.Text.Json;
using AiCodeGuard.Core;
using AiCodeGuard.Web.Data;
using AiCodeGuard.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AiCodeGuard.Web.Services;

/// <summary>
/// Orchestration d'une analyse : règles déterministes, relecture LLM optionnelle,
/// puis enregistrement dans l'historique. Les contrôleurs restent ainsi très fins.
/// </summary>
public class AnalysisService
{
    private readonly RuleEngine _engine;
    private readonly ILlmReviewer _llmReviewer;
    private readonly AppDbContext _db;
    private readonly ILogger<AnalysisService> _logger;

    public AnalysisService(RuleEngine engine, ILlmReviewer llmReviewer, AppDbContext db, ILogger<AnalysisService> logger)
    {
        _engine = engine;
        _llmReviewer = llmReviewer;
        _db = db;
        _logger = logger;
    }

    public bool LlmAvailable => _llmReviewer.IsEnabled;

    public async Task<AnalysisResultDto> AnalyzeAsync(AnalyzeRequest request, CancellationToken cancellationToken)
    {
        var file = new SourceFile(request.FileName ?? "extrait.cs", request.Code);
        var report = _engine.Analyze(file);

        string? llmReview = null;
        if (request.UseLlm && _llmReviewer.IsEnabled)
        {
            llmReview = await _llmReviewer.ReviewAsync(file, report, cancellationToken);
        }

        var findings = report.Findings.Select(ToDto).ToList();

        var record = new AnalysisRecord
        {
            FileName = report.FileName,
            SourceCode = request.Code,
            Score = report.Score,
            Verdict = report.Verdict.ToString(),
            FindingsCount = report.Findings.Count,
            CriticalCount = report.CountBySeverity(Severity.Critical),
            FindingsJson = JsonSerializer.Serialize(findings),
            LlmReview = llmReview,
            CreatedAt = report.AnalyzedAt
        };

        _db.Analyses.Add(record);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Analyse {Id} : {File} — score {Score}, verdict {Verdict}, {Count} problème(s)",
            record.Id, record.FileName, record.Score, record.Verdict, record.FindingsCount);

        return new AnalysisResultDto(
            record.Id, report.FileName, report.Score, report.Verdict.ToString(), report.Verdict.ToFrench(),
            report.LineCount, findings, llmReview, record.CreatedAt);
    }

    public async Task<IReadOnlyList<AnalysisRecord>> GetHistoryAsync(int take, CancellationToken cancellationToken) =>
        await _db.Analyses
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<AnalysisResultDto?> GetAsync(int id, CancellationToken cancellationToken)
    {
        var record = await _db.Analyses.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (record is null) return null;

        var findings = JsonSerializer.Deserialize<List<FindingDto>>(record.FindingsJson) ?? new List<FindingDto>();
        var verdict = Enum.TryParse<Verdict>(record.Verdict, out var v) ? v : Verdict.NeedsReview;
        var lineCount = record.SourceCode.Split('\n').Length;

        return new AnalysisResultDto(
            record.Id, record.FileName, record.Score, record.Verdict, verdict.ToFrench(),
            lineCount, findings, record.LlmReview, record.CreatedAt);
    }

    public string? GetSource(int id) =>
        _db.Analyses.AsNoTracking().Where(a => a.Id == id).Select(a => a.SourceCode).FirstOrDefault();

    public IReadOnlyList<RuleInfo> GetRules() =>
        _engine.Rules
            .Select(r => new RuleInfo(r.Id, r.Title, r.Severity.ToString(), r.Severity.ToFrench(), (int)r.Severity, r.Rationale))
            .OrderBy(r => r.Id)
            .ToList();

    private static FindingDto ToDto(Finding f) =>
        new(f.RuleId, f.Title, f.Severity.ToString(), f.Severity.ToFrench(), f.Line, f.Message, f.Recommendation, f.Snippet);
}
