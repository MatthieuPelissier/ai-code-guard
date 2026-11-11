namespace AiCodeGuard.Core;

/// <summary>
/// Deuxième niveau de contrôle, optionnel : une relecture par un LLM.
/// Le moteur de règles reste la référence (déterministe et testable) ;
/// le LLM apporte un avis complémentaire, jamais une décision.
/// </summary>
public interface ILlmReviewer
{
    bool IsEnabled { get; }

    Task<string?> ReviewAsync(SourceFile file, AnalysisReport report, CancellationToken cancellationToken = default);
}

/// <summary>Implémentation neutre utilisée quand aucune clé d'API n'est configurée (pattern Null Object).</summary>
public sealed class NullLlmReviewer : ILlmReviewer
{
    public bool IsEnabled => false;

    public Task<string?> ReviewAsync(SourceFile file, AnalysisReport report, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);
}
