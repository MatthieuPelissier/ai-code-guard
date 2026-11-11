using System.Text.RegularExpressions;

namespace AiCodeGuard.Core.Rules;

/// <summary>AICG001 — Secret (mot de passe, clé d'API, jeton) écrit en dur dans le code.</summary>
public sealed class HardcodedSecretRule : LineRegexRule
{
    private static readonly IReadOnlyList<Regex> _patterns = new[]
    {
        // password = "...", apiKey: "...", Token = @"..."
        RxI(@"\b\w*(password|passwd|pwd|secret|api_?key|apikey|access_?token|token)\w*\b\s*[:=]\s*@?""[^""\s]{4,}"""),
        // Mot de passe dans une chaîne de connexion : "Server=...;Password=xxx;"
        RxI(@"""[^""]*\b(password|pwd)\s*=\s*[^;""\s]{3,}"),
        // Formats de clés connus (OpenAI / Anthropic / AWS / GitHub)
        Rx(@"\b(sk-[A-Za-z0-9_\-]{20,}|AKIA[0-9A-Z]{16}|ghp_[A-Za-z0-9]{30,})\b")
    };

    public override string Id => "AICG001";
    public override string Title => "Secret écrit en dur";
    public override Severity Severity => Severity.Critical;
    public override string Rationale =>
        "Les assistants IA complètent volontiers les exemples avec de faux (ou de vrais) identifiants. " +
        "Un secret commité dans Git doit être considéré comme compromis.";
    protected override string Recommendation =>
        "Déplacer la valeur dans la configuration (User Secrets, variables d'environnement, coffre-fort) et la révoquer si elle a été partagée.";
    protected override IReadOnlyList<Regex> Patterns => _patterns;
}

/// <summary>AICG002 — Requête SQL construite par concaténation ou interpolation.</summary>
public sealed class SqlInjectionRule : LineRegexRule
{
    // Motifs SQL suffisamment spécifiques pour éviter les faux positifs sur du texte libre.
    private const string SqlKeywords =
        @"\b(SELECT\s+[^""]+?\s+FROM|INSERT\s+INTO|UPDATE\s+\S+\s+SET|DELETE\s+FROM|EXEC(UTE)?\s+\w+)\b";

    private static readonly IReadOnlyList<Regex> _patterns = new[]
    {
        // $"SELECT * FROM Users WHERE Id = {id}"
        RxI(@"\$@?""[^""]*" + SqlKeywords + @"[^""]*\{[^}]+\}"),
        // "SELECT ... WHERE Name = '" + name + "'"
        RxI(@"""[^""]*" + SqlKeywords + @"[^""]*""\s*\+"),
        // string.Format("SELECT ... {0}", value)
        RxI(@"string\.Format\(\s*""[^""]*" + SqlKeywords)
    };

    public override string Id => "AICG002";
    public override string Title => "Risque d'injection SQL";
    public override Severity Severity => Severity.Critical;
    public override string Rationale =>
        "Les LLM produisent fréquemment des requêtes SQL interpolées, plus courtes à écrire " +
        "mais vulnérables à l'injection (OWASP A03).";
    protected override string Recommendation =>
        "Utiliser des requêtes paramétrées (SqlParameter, Dapper, Entity Framework) au lieu de concaténer les valeurs.";
    protected override IReadOnlyList<Regex> Patterns => _patterns;
}

/// <summary>AICG009 — Algorithme de hachage obsolète (MD5, SHA1).</summary>
public sealed class WeakCryptographyRule : LineRegexRule
{
    private static readonly IReadOnlyList<Regex> _patterns = new[]
    {
        Rx(@"\b(MD5|SHA1)\s*\.\s*(Create|HashData)\s*\("),
        Rx(@"\bnew\s+(MD5CryptoServiceProvider|SHA1Managed|SHA1CryptoServiceProvider)\b")
    };

    public override string Id => "AICG009";
    public override string Title => "Cryptographie obsolète";
    public override Severity Severity => Severity.Error;
    public override string Rationale =>
        "Entraînés sur du code ancien, les modèles proposent encore MD5/SHA1, " +
        "qui ne doivent plus être utilisés pour la sécurité (mots de passe, signatures).";
    protected override string Recommendation =>
        "Utiliser SHA256 au minimum, et un algorithme dédié (PBKDF2, BCrypt, Argon2) pour les mots de passe.";
    protected override IReadOnlyList<Regex> Patterns => _patterns;
}
