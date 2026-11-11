using System.Text.RegularExpressions;

namespace AiCodeGuard.Core.Rules;

/// <summary>AICG003 — Bloc catch vide : l'erreur est avalée silencieusement.</summary>
public sealed class EmptyCatchRule : IAnalysisRule
{
    private static readonly Regex Pattern = new(
        @"catch\s*(\([^)]*\))?\s*\{\s*\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(250));

    public string Id => "AICG003";
    public string Title => "Bloc catch vide";
    public Severity Severity => Severity.Error;
    public string Rationale =>
        "Pour « faire compiler » ou « éviter un crash », l'IA ajoute parfois un try/catch vide " +
        "qui masque les erreurs et rend la maintenance de niveau 3 très difficile.";

    public IEnumerable<Finding> Analyze(SourceFile file)
    {
        // Analyse sur le contenu complet : le bloc peut s'étendre sur plusieurs lignes.
        foreach (Match match in Pattern.Matches(file.NormalizedContent))
        {
            var line = file.LineNumberAt(match.Index);
            yield return new Finding(
                Id, Title, Severity, line,
                "Une exception est interceptée puis ignorée.",
                "Journaliser l'erreur (ILogger), la relancer, ou gérer explicitement le cas attendu.",
                LineRegexRule.Truncate(match.Value.Replace("\n", " "), 160));
        }
    }
}

/// <summary>AICG004 — Interception générique de System.Exception.</summary>
public sealed class GenericCatchRule : LineRegexRule
{
    private static readonly IReadOnlyList<Regex> _patterns = new[]
    {
        Rx(@"catch\s*\(\s*(System\.)?Exception(\s+\w+)?\s*\)")
    };

    public override string Id => "AICG004";
    public override string Title => "Exception générique interceptée";
    public override Severity Severity => Severity.Warning;
    public override string Rationale =>
        "catch (Exception) est la réponse par défaut des modèles. Elle intercepte aussi les erreurs " +
        "qui devraient remonter (OutOfMemory, annulation, bugs).";
    protected override string Recommendation =>
        "Intercepter les exceptions précises attendues ; réserver la capture générique à la frontière de l'application (middleware).";
    protected override IReadOnlyList<Regex> Patterns => _patterns;
}

/// <summary>AICG005 — Méthode async void (hors gestionnaire d'événement).</summary>
public sealed class AsyncVoidRule : LineRegexRule
{
    private static readonly IReadOnlyList<Regex> _patterns = new[]
    {
        Rx(@"\basync\s+void\s+(\w+)\s*\(")
    };

    public override string Id => "AICG005";
    public override string Title => "Méthode async void";
    public override Severity Severity => Severity.Error;
    public override string Rationale =>
        "Une méthode async void ne peut pas être attendue et ses exceptions font planter le processus.";
    protected override string Recommendation => "Retourner Task (ou Task<T>) et faire un await à l'appel.";
    protected override IReadOnlyList<Regex> Patterns => _patterns;

    // Les gestionnaires d'événements (object sender, EventArgs e) sont l'exception admise.
    protected override bool IsFalsePositive(string line, Match match) =>
        line.Contains("EventArgs", StringComparison.Ordinal);

    protected override string BuildMessage(Match match) =>
        $"La méthode « {match.Groups[1].Value} » est déclarée async void.";
}

/// <summary>AICG007 — Blocage d'un appel asynchrone (.Result, .Wait()).</summary>
public sealed class BlockingAsyncRule : LineRegexRule
{
    private static readonly IReadOnlyList<Regex> _patterns = new[]
    {
        Rx(@"\)\s*\.Result\b"),
        Rx(@"Async\([^)]*\)\s*\.Wait\(\s*\)"),
        Rx(@"\.GetAwaiter\(\)\s*\.GetResult\(\)")
    };

    public override string Id => "AICG007";
    public override string Title => "Appel asynchrone bloquant";
    public override Severity Severity => Severity.Warning;
    public override string Rationale =>
        "Mélanger code synchrone et asynchrone (.Result, .Wait()) provoque des interblocages en ASP.NET " +
        "et dégrade les performances.";
    protected override string Recommendation => "Propager l'asynchronisme avec async/await jusqu'à l'appelant.";
    protected override IReadOnlyList<Regex> Patterns => _patterns;
}
