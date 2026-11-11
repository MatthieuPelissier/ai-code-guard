using System.Text.RegularExpressions;

namespace AiCodeGuard.Core.Rules;

/// <summary>
/// AICG006 — Code incomplet laissé par l'IA : troncature (« ... reste du code »),
/// TODO, NotImplementedException.
/// </summary>
public sealed class AiPlaceholderRule : LineRegexRule
{
    private static readonly IReadOnlyList<Regex> _patterns = new[]
    {
        RxI(@"//\s*(\.\.\.|…)"),
        RxI(@"\b(TODO|FIXME|HACK)\b"),
        RxI(@"(rest of (the )?(code|implementation)|reste du code|your (code|logic) here|votre code ici|implement(ation)? (here|ici)|à compléter|logique (métier )?ici|same as above|comme ci-dessus)"),
        Rx(@"throw\s+new\s+NotImplementedException\s*\(")
    };

    public override string Id => "AICG006";
    public override string Title => "Code incomplet (trace de génération IA)";
    public override Severity Severity => Severity.Error;
    public override string Rationale =>
        "Les LLM tronquent souvent leurs réponses (« // ... reste du code ») ou laissent des " +
        "squelettes à compléter. Ce code compile parfois, mais il est faux.";
    protected override string Recommendation =>
        "Compléter réellement l'implémentation ou redemander la partie manquante, puis couvrir par un test unitaire.";
    protected override IReadOnlyList<Regex> Patterns => _patterns;

    // Les marqueurs de troncature se trouvent justement dans les commentaires.
    protected override bool IgnoreCommentLines => false;
}

/// <summary>
/// AICG008 — Espace de noms inconnu : possible bibliothèque « hallucinée » par l'IA.
/// </summary>
public sealed class UnknownNamespaceRule : LineRegexRule
{
    /// <summary>Racines d'espaces de noms validées par l'équipe (configurable).</summary>
    public static readonly IReadOnlyCollection<string> DefaultAllowedRoots = new[]
    {
        "System", "Microsoft", "AiCodeGuard", "Xunit", "NUnit", "Moq", "NSubstitute",
        "FluentAssertions", "Newtonsoft", "Dapper", "Serilog", "AutoMapper", "MediatR", "Polly"
    };

    private static readonly IReadOnlyList<Regex> _patterns = new[]
    {
        Rx(@"^\s*(global\s+)?using\s+(static\s+)?([A-Za-z_][\w]*)(\.[\w.]+)?\s*;")
    };

    private readonly HashSet<string> _allowedRoots;

    public UnknownNamespaceRule() : this(DefaultAllowedRoots) { }

    public UnknownNamespaceRule(IEnumerable<string> allowedRoots)
    {
        _allowedRoots = new HashSet<string>(allowedRoots, StringComparer.Ordinal);
    }

    public override string Id => "AICG008";
    public override string Title => "Dépendance inconnue (hallucination possible)";
    public override Severity Severity => Severity.Warning;
    public override string Rationale =>
        "Les modèles inventent parfois des packages ou des espaces de noms plausibles mais inexistants. " +
        "Des attaquants publient ensuite ces noms sur NuGet (« slopsquatting »).";
    protected override string Recommendation =>
        "Vérifier que le package existe sur NuGet, qu'il est maintenu et validé par l'équipe, puis l'ajouter à la liste autorisée.";
    protected override IReadOnlyList<Regex> Patterns => _patterns;

    protected override bool IsFalsePositive(string line, Match match) =>
        _allowedRoots.Contains(match.Groups[3].Value);

    protected override string BuildMessage(Match match) =>
        $"L'espace de noms « {match.Groups[3].Value}{match.Groups[4].Value} » n'est pas dans la liste des dépendances validées.";
}

/// <summary>AICG010 — Sortie console de débogage laissée dans le code.</summary>
public sealed class DebugOutputRule : LineRegexRule
{
    private static readonly IReadOnlyList<Regex> _patterns = new[]
    {
        Rx(@"\bConsole\.Write(Line)?\s*\("),
        Rx(@"\bDebug\.Write(Line)?\s*\(")
    };

    public override string Id => "AICG010";
    public override string Title => "Sortie console de débogage";
    public override Severity Severity => Severity.Info;
    public override string Rationale =>
        "Le code généré pour « montrer que ça marche » contient souvent des Console.WriteLine " +
        "qui n'ont rien à faire dans une application web.";
    protected override string Recommendation => "Utiliser ILogger<T> avec un niveau de log adapté.";
    protected override IReadOnlyList<Regex> Patterns => _patterns;
}
