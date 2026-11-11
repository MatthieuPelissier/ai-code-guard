using System.Text.RegularExpressions;

namespace AiCodeGuard.Core.Rules;

/// <summary>
/// Classe de base pour les règles qui inspectent le code ligne par ligne
/// (design pattern Template Method : l'algorithme est fixé ici,
/// les règles concrètes ne fournissent que leurs motifs et leurs messages).
/// </summary>
public abstract class LineRegexRule : IAnalysisRule
{
    protected static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);

    public abstract string Id { get; }
    public abstract string Title { get; }
    public abstract Severity Severity { get; }
    public abstract string Rationale { get; }

    /// <summary>Recommandation affichée au développeur.</summary>
    protected abstract string Recommendation { get; }

    /// <summary>Motifs recherchés sur chaque ligne.</summary>
    protected abstract IReadOnlyList<Regex> Patterns { get; }

    /// <summary>Faut-il ignorer les lignes de commentaire ? (vrai par défaut)</summary>
    protected virtual bool IgnoreCommentLines => true;

    /// <summary>Permet à une règle d'écarter un faux positif connu.</summary>
    protected virtual bool IsFalsePositive(string line, Match match) => false;

    protected virtual string BuildMessage(Match match) => Title;

    public IEnumerable<Finding> Analyze(SourceFile file)
    {
        for (var i = 0; i < file.Lines.Count; i++)
        {
            var line = file.Lines[i];
            if (IgnoreCommentLines && IsCommentLine(line)) continue;

            foreach (var pattern in Patterns)
            {
                var match = pattern.Match(line);
                if (!match.Success || IsFalsePositive(line, match)) continue;

                yield return new Finding(
                    Id,
                    Title,
                    Severity,
                    i + 1,
                    BuildMessage(match),
                    Recommendation,
                    Truncate(line.Trim(), 160));
                break; // un seul signalement par ligne et par règle
            }
        }
    }

    protected static Regex Rx(string pattern) =>
        new(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant, RegexTimeout);

    protected static Regex RxI(string pattern) =>
        new(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, RegexTimeout);

    internal static bool IsCommentLine(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.StartsWith("//", StringComparison.Ordinal)
            || trimmed.StartsWith("/*", StringComparison.Ordinal)
            || trimmed.StartsWith("*", StringComparison.Ordinal);
    }

    internal static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}
