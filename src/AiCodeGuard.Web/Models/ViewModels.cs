using System.ComponentModel.DataAnnotations;

namespace AiCodeGuard.Web.Models;

public class AnalyzeRequest
{
    [StringLength(260)]
    public string? FileName { get; set; }

    [Required(ErrorMessage = "Le code à analyser est obligatoire.")]
    [StringLength(200_000, ErrorMessage = "Le code ne doit pas dépasser 200 000 caractères.")]
    public string Code { get; set; } = string.Empty;

    public bool UseLlm { get; set; }
}

public record FindingDto(
    string RuleId,
    string Title,
    string Severity,
    string SeverityLabel,
    int Line,
    string Message,
    string Recommendation,
    string Snippet);

public record AnalysisResultDto(
    int Id,
    string FileName,
    int Score,
    string Verdict,
    string VerdictLabel,
    int LineCount,
    IReadOnlyList<FindingDto> Findings,
    string? LlmReview,
    DateTime CreatedAt);

public record RuleInfo(string Id, string Title, string Severity, string SeverityLabel, int Penalty, string Rationale);
