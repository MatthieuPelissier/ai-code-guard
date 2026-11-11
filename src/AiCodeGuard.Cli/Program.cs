using System.Text;
using AiCodeGuard.Core;

// AI Code Guard — outil en ligne de commande, pensé pour la CI (barrière qualité avant fusion).
//
//   aicodeguard <fichier|dossier> [--min-score 80] [--report rapport.md]
//
// Code de sortie : 0 = tout est accepté, 1 = au moins un fichier rejeté ou sous le seuil, 2 = erreur d'usage.

if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
{
    PrintUsage();
    return args.Length == 0 ? 2 : 0;
}

var target = args[0];
var minScore = RuleEngine.AcceptThreshold;
string? reportPath = null;

for (var i = 1; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--min-score" when i + 1 < args.Length && int.TryParse(args[i + 1], out var parsed):
            minScore = Math.Clamp(parsed, 0, 100);
            i++;
            break;
        case "--report" when i + 1 < args.Length:
            reportPath = args[i + 1];
            i++;
            break;
        default:
            Console.Error.WriteLine($"Option inconnue ou incomplète : {args[i]}");
            PrintUsage();
            return 2;
    }
}

IReadOnlyList<string> files;
if (File.Exists(target))
{
    files = new[] { target };
}
else if (Directory.Exists(target))
{
    files = Directory
        .EnumerateFiles(target, "*.cs", SearchOption.AllDirectories)
        .Where(f => !IsBuildOutput(f))
        .OrderBy(f => f, StringComparer.Ordinal)
        .ToList();
}
else
{
    Console.Error.WriteLine($"Chemin introuvable : {target}");
    return 2;
}

if (files.Count == 0)
{
    Console.WriteLine("Aucun fichier .cs à analyser.");
    return 0;
}

var engine = DefaultRules.CreateEngine();
var reports = files
    .Select(path => engine.Analyze(new SourceFile(Path.GetRelativePath(Directory.GetCurrentDirectory(), path), File.ReadAllText(path))))
    .ToList();

foreach (var report in reports)
{
    WriteColored($"{VerdictIcon(report.Verdict)} {report.FileName}  —  {report.Score}/100  ({report.Verdict.ToFrench()})", VerdictColor(report.Verdict));
    foreach (var f in report.Findings)
    {
        Console.WriteLine($"    L{f.Line,-4} {f.RuleId}  [{f.Severity.ToFrench()}]  {f.Message}");
    }
}

var failed = reports.Where(r => r.Verdict == Verdict.Rejected || r.Score < minScore).ToList();
var average = (int)Math.Round(reports.Average(r => r.Score));

Console.WriteLine();
Console.WriteLine($"{reports.Count} fichier(s) analysé(s) · score moyen {average}/100 · seuil {minScore} · {failed.Count} en échec");

if (reportPath is not null)
{
    File.WriteAllText(reportPath, BuildMarkdown(reports, minScore, average), Encoding.UTF8);
    Console.WriteLine($"Rapport écrit : {reportPath}");
}

return failed.Count == 0 ? 0 : 1;

static bool IsBuildOutput(string path)
{
    var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    return parts.Any(p => p is "bin" or "obj" or ".git");
}

static string VerdictIcon(Verdict v) => v switch
{
    Verdict.Accepted => "[OK]",
    Verdict.NeedsReview => "[??]",
    _ => "[KO]"
};

static ConsoleColor VerdictColor(Verdict v) => v switch
{
    Verdict.Accepted => ConsoleColor.Green,
    Verdict.NeedsReview => ConsoleColor.Yellow,
    _ => ConsoleColor.Red
};

static void WriteColored(string text, ConsoleColor color)
{
    var previous = Console.ForegroundColor;
    Console.ForegroundColor = color;
    Console.WriteLine(text);
    Console.ForegroundColor = previous;
}

static string BuildMarkdown(IReadOnlyList<AnalysisReport> reports, int minScore, int average)
{
    var sb = new StringBuilder();
    sb.AppendLine("# Rapport AI Code Guard");
    sb.AppendLine();
    sb.AppendLine($"- Date : {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
    sb.AppendLine($"- Fichiers analysés : {reports.Count}");
    sb.AppendLine($"- Score moyen : {average}/100 (seuil : {minScore})");
    sb.AppendLine();
    sb.AppendLine("| Fichier | Score | Verdict | Problèmes |");
    sb.AppendLine("|---|---:|---|---:|");
    foreach (var r in reports)
    {
        sb.AppendLine($"| `{r.FileName}` | {r.Score} | {r.Verdict.ToFrench()} | {r.Findings.Count} |");
    }

    foreach (var r in reports.Where(x => x.Findings.Count > 0))
    {
        sb.AppendLine();
        sb.AppendLine($"## {r.FileName}");
        sb.AppendLine();
        foreach (var f in r.Findings)
        {
            sb.AppendLine($"- **L{f.Line} · {f.RuleId} · {f.Title}** ({f.Severity.ToFrench()}) — {f.Message}  ");
            sb.AppendLine($"  _Correction :_ {f.Recommendation}");
        }
    }
    return sb.ToString();
}

static void PrintUsage()
{
    Console.WriteLine("AI Code Guard — contrôle du code généré par IA");
    Console.WriteLine();
    Console.WriteLine("Usage : aicodeguard <fichier|dossier> [--min-score 80] [--report rapport.md]");
    Console.WriteLine();
    Console.WriteLine("Codes de sortie : 0 = accepté · 1 = rejeté ou sous le seuil · 2 = erreur d'usage");
}
