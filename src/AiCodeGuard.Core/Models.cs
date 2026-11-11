namespace AiCodeGuard.Core;

/// <summary>
/// Gravité d'un problème détecté. La valeur numérique correspond à la pénalité
/// retirée du score de confiance (sur 100).
/// </summary>
public enum Severity
{
    Info = 1,
    Warning = 5,
    Error = 12,
    Critical = 30
}

/// <summary>Décision finale sur le code analysé.</summary>
public enum Verdict
{
    Accepted,
    NeedsReview,
    Rejected
}

/// <summary>Fichier source soumis à l'analyse (souvent du code généré par une IA).</summary>
public sealed class SourceFile
{
    public SourceFile(string name, string content)
    {
        Name = string.IsNullOrWhiteSpace(name) ? "extrait.cs" : name;
        Content = content ?? string.Empty;
        NormalizedContent = Content.Replace("\r\n", "\n");
        Lines = NormalizedContent.Split('\n');
    }

    public string Name { get; }
    public string Content { get; }

    /// <summary>Contenu avec fins de ligne unifiées (\n), utilisé pour les analyses multi-lignes.</summary>
    public string NormalizedContent { get; }
    public IReadOnlyList<string> Lines { get; }

    /// <summary>Convertit une position dans NormalizedContent en numéro de ligne (base 1).</summary>
    public int LineNumberAt(int index)
    {
        var normalized = NormalizedContent;
        var safeIndex = Math.Clamp(index, 0, normalized.Length);
        var line = 1;
        for (var i = 0; i < safeIndex; i++)
        {
            if (normalized[i] == '\n') line++;
        }
        return line;
    }
}

/// <summary>Problème détecté par une règle.</summary>
public sealed record Finding(
    string RuleId,
    string Title,
    Severity Severity,
    int Line,
    string Message,
    string Recommendation,
    string Snippet);

/// <summary>Résultat complet de l'analyse d'un fichier.</summary>
public sealed class AnalysisReport
{
    public AnalysisReport(string fileName, int lineCount, IReadOnlyList<Finding> findings, int score, Verdict verdict)
    {
        FileName = fileName;
        LineCount = lineCount;
        Findings = findings;
        Score = score;
        Verdict = verdict;
        AnalyzedAt = DateTime.UtcNow;
    }

    public string FileName { get; }
    public int LineCount { get; }
    public IReadOnlyList<Finding> Findings { get; }
    public int Score { get; }
    public Verdict Verdict { get; }
    public DateTime AnalyzedAt { get; }

    public int CountBySeverity(Severity severity) => Findings.Count(f => f.Severity == severity);
}

public static class LabelExtensions
{
    public static string ToFrench(this Verdict verdict) => verdict switch
    {
        Verdict.Accepted => "Accepté",
        Verdict.NeedsReview => "À revoir",
        Verdict.Rejected => "Rejeté",
        _ => verdict.ToString()
    };

    public static string ToFrench(this Severity severity) => severity switch
    {
        Severity.Info => "Info",
        Severity.Warning => "Avertissement",
        Severity.Error => "Erreur",
        Severity.Critical => "Critique",
        _ => severity.ToString()
    };
}
