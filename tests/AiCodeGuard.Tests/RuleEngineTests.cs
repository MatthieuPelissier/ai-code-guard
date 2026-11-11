using AiCodeGuard.Core;

namespace AiCodeGuard.Tests;

public class RuleEngineTests
{
    private readonly RuleEngine _engine = DefaultRules.CreateEngine();

    private const string CleanCode = """
        using Microsoft.Extensions.Logging;

        namespace Demo;

        public sealed class PriceCalculator
        {
            private readonly ILogger<PriceCalculator> _logger;

            public PriceCalculator(ILogger<PriceCalculator> logger) => _logger = logger;

            public decimal WithVat(decimal amount, decimal rate)
            {
                if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
                var total = amount * (1 + rate);
                _logger.LogDebug("Prix TTC calculé : {Total}", total);
                return total;
            }
        }
        """;

    private const string RiskyAiCode = """
        using System.Data.SqlClient;

        public class Repo
        {
            private string password = "Admin2024!";

            public void Load(string id)
            {
                var cmd = new SqlCommand($"SELECT * FROM Users WHERE Id = {id}");
                // ... reste du code
            }
        }
        """;

    [Fact]
    public void CleanCode_IsAccepted_WithFullScore()
    {
        var report = _engine.Analyze(new SourceFile("PriceCalculator.cs", CleanCode));

        Assert.Empty(report.Findings);
        Assert.Equal(100, report.Score);
        Assert.Equal(Verdict.Accepted, report.Verdict);
    }

    [Fact]
    public void RiskyAiCode_IsRejected()
    {
        var report = _engine.Analyze(new SourceFile("Repo.cs", RiskyAiCode));

        Assert.Equal(Verdict.Rejected, report.Verdict);
        Assert.Contains(report.Findings, f => f.RuleId == "AICG001");
        Assert.Contains(report.Findings, f => f.RuleId == "AICG002");
        Assert.Contains(report.Findings, f => f.RuleId == "AICG006");
        Assert.True(report.Score < RuleEngine.ReviewThreshold);
    }

    [Fact]
    public void Findings_AreSortedByLine()
    {
        var report = _engine.Analyze(new SourceFile("Repo.cs", RiskyAiCode));
        var lines = report.Findings.Select(f => f.Line).ToList();

        Assert.Equal(lines.OrderBy(l => l).ToList(), lines);
    }

    [Fact]
    public void Score_NeverGoesBelowZero()
    {
        var manyProblems = string.Join("\n", Enumerable.Repeat("var password = \"Secret123\";", 10));
        var report = _engine.Analyze(new SourceFile("Bad.cs", manyProblems));

        Assert.Equal(0, report.Score);
    }

    [Fact]
    public void OneCriticalFinding_RejectsEvenWithHighScore()
    {
        var findings = new[] { new Finding("X", "x", Severity.Critical, 1, "", "", "") };
        Assert.Equal(Verdict.Rejected, RuleEngine.ComputeVerdict(findings, 90));
    }

    [Theory]
    [InlineData(100, Verdict.Accepted)]
    [InlineData(80, Verdict.Accepted)]
    [InlineData(79, Verdict.NeedsReview)]
    [InlineData(50, Verdict.NeedsReview)]
    [InlineData(49, Verdict.Rejected)]
    public void Verdict_FollowsThresholds(int score, Verdict expected)
    {
        Assert.Equal(expected, RuleEngine.ComputeVerdict(Array.Empty<Finding>(), score));
    }

    [Fact]
    public void Engine_UsesAllDefaultRules_WithUniqueIds()
    {
        var ids = _engine.Rules.Select(r => r.Id).ToList();

        Assert.Equal(10, ids.Count);
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void SourceFile_HandlesWindowsLineEndings()
    {
        var file = new SourceFile("a.cs", "ligne1\r\nligne2\r\nligne3");

        Assert.Equal(3, file.Lines.Count);
        Assert.Equal(3, file.LineNumberAt(file.NormalizedContent.IndexOf("ligne3", StringComparison.Ordinal)));
    }
}
