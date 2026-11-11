namespace AiCodeGuard.Core;

/// <summary>
/// Contrat d'une règle de contrôle (design pattern Strategy) :
/// chaque règle est indépendante, testable seule, et peut être ajoutée
/// ou retirée du moteur sans modifier le reste du code (principe Open/Closed).
/// </summary>
public interface IAnalysisRule
{
    /// <summary>Identifiant stable, ex. AICG001.</summary>
    string Id { get; }

    string Title { get; }

    Severity Severity { get; }

    /// <summary>Pourquoi cette règle existe, en particulier face au code généré par IA.</summary>
    string Rationale { get; }

    IEnumerable<Finding> Analyze(SourceFile file);
}
