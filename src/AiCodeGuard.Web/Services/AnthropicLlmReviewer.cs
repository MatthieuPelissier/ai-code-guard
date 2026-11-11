using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AiCodeGuard.Core;
using Microsoft.Extensions.Options;

namespace AiCodeGuard.Web.Services;

public class LlmOptions
{
    public const string SectionName = "Llm";

    public string? ApiKey { get; set; }
    public string Model { get; set; } = "claude-sonnet-5-5";
    public string Endpoint { get; set; } = "https://api.anthropic.com/v1/messages";
    public int MaxTokens { get; set; } = 1024;
}

/// <summary>
/// Relecture complémentaire par un LLM (API Anthropic Messages).
/// Le prompt transmet les résultats du moteur de règles pour éviter les doublons
/// et demande une réponse courte, structurée et vérifiable.
/// </summary>
public class AnthropicLlmReviewer : ILlmReviewer
{
    private readonly HttpClient _http;
    private readonly LlmOptions _options;
    private readonly ILogger<AnthropicLlmReviewer> _logger;

    public AnthropicLlmReviewer(HttpClient http, IOptions<LlmOptions> options, ILogger<AnthropicLlmReviewer> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
        _http.Timeout = TimeSpan.FromSeconds(60);
    }

    public bool IsEnabled => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<string?> ReviewAsync(SourceFile file, AnalysisReport report, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled) return null;

        var payload = new
        {
            model = _options.Model,
            max_tokens = _options.MaxTokens,
            system = SystemPrompt,
            messages = new[]
            {
                new { role = "user", content = BuildUserPrompt(file, report) }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("x-api-key", _options.ApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        try
        {
            using var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Relecture LLM indisponible : HTTP {Status}", (int)response.StatusCode);
                return $"Relecture LLM indisponible (HTTP {(int)response.StatusCode}).";
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var text = new StringBuilder();
            foreach (var block in json.RootElement.GetProperty("content").EnumerateArray())
            {
                if (block.TryGetProperty("type", out var type) && type.GetString() == "text")
                {
                    text.Append(block.GetProperty("text").GetString());
                }
            }
            return text.ToString();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Erreur réseau lors de la relecture LLM");
            return "Relecture LLM indisponible (erreur réseau).";
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Délai dépassé lors de la relecture LLM");
            return "Relecture LLM indisponible (délai dépassé).";
        }
    }

    private const string SystemPrompt =
        "Tu es relecteur de code senior C#/.NET 8 dans un éditeur de logiciels. " +
        "Le code soumis a probablement été généré par une IA. " +
        "Un analyseur statique a déjà produit une liste de problèmes : ne les répète pas. " +
        "Signale uniquement ce qu'il a manqué : erreurs de logique, cas limites non gérés, " +
        "API inexistantes ou mal utilisées, problèmes de sécurité ou de performance. " +
        "Réponds en français, en 5 points maximum, chacun sous la forme : " +
        "[ligne] problème — correction proposée. Si tu n'es pas sûr, dis-le explicitement.";

    private static string BuildUserPrompt(SourceFile file, AnalysisReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Fichier : {file.Name}");
        sb.AppendLine($"Score de l'analyseur : {report.Score}/100 ({report.Verdict.ToFrench()})");
        sb.AppendLine("Problèmes déjà détectés :");
        if (report.Findings.Count == 0) sb.AppendLine("- aucun");
        foreach (var f in report.Findings)
        {
            sb.AppendLine($"- L{f.Line} {f.RuleId} {f.Title}");
        }
        sb.AppendLine();
        sb.AppendLine("```csharp");
        sb.AppendLine(file.Content);
        sb.AppendLine("```");
        return sb.ToString();
    }
}
