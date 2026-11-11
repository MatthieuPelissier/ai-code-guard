using AiCodeGuard.Core.Rules;

namespace AiCodeGuard.Core;

/// <summary>
/// Moteur d'analyse : exécute toutes les règles, calcule le score de confiance
/// et rend un verdict (Accepté / À revoir / Rejeté).
/// </summary>
public sealed class RuleEngine
{
    public const int AcceptThreshold = 80;
    public const int ReviewThreshold = 50;

    private readonly IReadOnlyList<IAnalysisRule> _rules;

    public RuleEngine(IEnumerable<IAnalysisRule> rules)
    {
        _rules = rules?.ToList() ?? throw new ArgumentNullException(nameof(rules));
    }

    public IReadOnlyList<IAnalysisRule> Rules => _rules;

    public AnalysisReport Analyze(SourceFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        var findings = _rules
            .SelectMany(rule => rule.Analyze(file))
            .OrderBy(f => f.Line)
            .ThenByDescending(f => f.Severity)
            .ToList();

        var score = ComputeScore(findings);
        var verdict = ComputeVerdict(findings, score);

        return new AnalysisReport(file.Name, file.Lines.Count, findings, score, verdict);
    }

    public static int ComputeScore(IEnumerable<Finding> findings)
    {
        var penalty = findings.Sum(f => (int)f.Severity);
        return Math.Max(0, 100 - penalty);
    }

    public static Verdict ComputeVerdict(IReadOnlyCollection<Finding> findings, int score)
    {
        // Une seule faille critique suffit à bloquer le code, quel que soit le score.
        if (findings.Any(f => f.Severity == Severity.Critical)) return Verdict.Rejected;
        if (score >= AcceptThreshold) return Verdict.Accepted;
        if (score >= ReviewThreshold) return Verdict.NeedsReview;
        return Verdict.Rejected;
    }
}

/// <summary>Fabrique du jeu de règles par défaut (design pattern Factory).</summary>
public static class DefaultRules
{
    public static IReadOnlyList<IAnalysisRule> CreateAll() => new IAnalysisRule[]
    {
        new HardcodedSecretRule(),
        new SqlInjectionRule(),
        new EmptyCatchRule(),
        new GenericCatchRule(),
        new AsyncVoidRule(),
        new AiPlaceholderRule(),
        new BlockingAsyncRule(),
        new UnknownNamespaceRule(),
        new WeakCryptographyRule(),
        new DebugOutputRule()
    };

    public static RuleEngine CreateEngine() => new(CreateAll());
}
