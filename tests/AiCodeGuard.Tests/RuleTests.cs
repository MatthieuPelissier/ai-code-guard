using AiCodeGuard.Core;
using AiCodeGuard.Core.Rules;

namespace AiCodeGuard.Tests;

/// <summary>
/// Tests unitaires des règles : chaque règle doit détecter le cas fautif
/// ET ne pas signaler le code correct (pas de faux positif).
/// </summary>
public class RuleTests
{
    private static List<Finding> Run(IAnalysisRule rule, string code) =>
        rule.Analyze(new SourceFile("Test.cs", code)).ToList();

    // ---------- AICG001 : secrets ----------

    [Theory]
    [InlineData("private const string ApiKey = \"abcd1234efgh\";")]
    [InlineData("var password = \"Admin2024!\";")]
    [InlineData("var cs = \"Server=db;User Id=sa;Password=Secret123;\";")]
    [InlineData("client.Auth(\"sk-ant-api03-AbCdEfGhIjKlMnOpQrStUv\");")]
    public void HardcodedSecret_IsDetected(string line)
    {
        var findings = Run(new HardcodedSecretRule(), line);
        Assert.Single(findings);
        Assert.Equal("AICG001", findings[0].RuleId);
        Assert.Equal(Severity.Critical, findings[0].Severity);
    }

    [Theory]
    [InlineData("var apiKey = configuration[\"Llm:ApiKey\"];")]
    [InlineData("var password = Environment.GetEnvironmentVariable(\"DB_PASSWORD\");")]
    [InlineData("// password = \"exemple dans un commentaire\"")]
    public void HardcodedSecret_NoFalsePositive(string line)
    {
        Assert.Empty(Run(new HardcodedSecretRule(), line));
    }

    // ---------- AICG002 : injection SQL ----------

    [Theory]
    [InlineData("var cmd = new SqlCommand($\"SELECT * FROM Users WHERE Id = {id}\", cn);")]
    [InlineData("var sql = \"SELECT Name FROM Users WHERE Login = '\" + login + \"'\";")]
    [InlineData("var sql = \"DELETE FROM Orders WHERE Id = \" + id;")]
    [InlineData("var sql = string.Format(\"UPDATE Users SET Name = '{0}'\", name);")]
    public void SqlInjection_IsDetected(string line)
    {
        var findings = Run(new SqlInjectionRule(), line);
        Assert.Single(findings);
        Assert.Equal("AICG002", findings[0].RuleId);
    }

    [Theory]
    [InlineData("var cmd = new SqlCommand(\"SELECT * FROM Users WHERE Id = @id\", cn);")]
    [InlineData("_logger.LogInformation($\"Update of {count} items\");")]
    [InlineData("var message = $\"Bonjour {name}, sélectionnez une option\";")]
    public void SqlInjection_NoFalsePositive(string line)
    {
        Assert.Empty(Run(new SqlInjectionRule(), line));
    }

    // ---------- AICG003 : catch vide ----------

    [Fact]
    public void EmptyCatch_MultiLine_IsDetectedWithCorrectLine()
    {
        const string code = "try\n{\n    Do();\n}\ncatch (Exception)\n{\n}\n";
        var findings = Run(new EmptyCatchRule(), code);
        Assert.Single(findings);
        Assert.Equal(5, findings[0].Line);
    }

    [Fact]
    public void EmptyCatch_InlineBraces_IsDetected()
    {
        Assert.Single(Run(new EmptyCatchRule(), "try { Do(); } catch { }"));
    }

    [Fact]
    public void Catch_WithLogging_IsNotEmpty()
    {
        const string code = "try { Do(); } catch (IOException ex) { _logger.LogError(ex, \"Echec\"); throw; }";
        Assert.Empty(Run(new EmptyCatchRule(), code));
    }

    // ---------- AICG004 : catch générique ----------

    [Theory]
    [InlineData("catch (Exception ex)")]
    [InlineData("catch(System.Exception)")]
    public void GenericCatch_IsDetected(string line)
    {
        Assert.Single(Run(new GenericCatchRule(), line));
    }

    [Fact]
    public void SpecificCatch_IsAccepted()
    {
        Assert.Empty(Run(new GenericCatchRule(), "catch (InvalidOperationException ex)"));
    }

    // ---------- AICG005 : async void ----------

    [Fact]
    public void AsyncVoid_IsDetected()
    {
        var findings = Run(new AsyncVoidRule(), "public async void SaveAsync(User user)");
        Assert.Single(findings);
        Assert.Contains("SaveAsync", findings[0].Message);
    }

    [Fact]
    public void AsyncVoid_EventHandler_IsAllowed()
    {
        Assert.Empty(Run(new AsyncVoidRule(), "private async void OnClick(object sender, EventArgs e)"));
    }

    // ---------- AICG006 : code incomplet ----------

    [Theory]
    [InlineData("// ... reste du code")]
    [InlineData("    // ...")]
    [InlineData("// TODO: gérer les erreurs")]
    [InlineData("// Implement here")]
    [InlineData("throw new NotImplementedException();")]
    public void AiPlaceholder_IsDetected(string line)
    {
        Assert.Single(Run(new AiPlaceholderRule(), line));
    }

    [Fact]
    public void AiPlaceholder_NormalComment_IsAccepted()
    {
        Assert.Empty(Run(new AiPlaceholderRule(), "// Calcule le total TTC de la commande"));
    }

    // ---------- AICG007 : appels bloquants ----------

    [Theory]
    [InlineData("var user = GetUserAsync(id).Result;")]
    [InlineData("SaveAsync(order).Wait();")]
    [InlineData("var x = LoadAsync().GetAwaiter().GetResult();")]
    public void BlockingAsync_IsDetected(string line)
    {
        Assert.Single(Run(new BlockingAsyncRule(), line));
    }

    [Fact]
    public void AwaitedCall_IsAccepted()
    {
        Assert.Empty(Run(new BlockingAsyncRule(), "var user = await GetUserAsync(id);"));
    }

    // ---------- AICG008 : dépendances inconnues ----------

    [Fact]
    public void UnknownNamespace_IsDetected()
    {
        var findings = Run(new UnknownNamespaceRule(), "using FastSql.AutoMapper.Extensions;");
        Assert.Single(findings);
        Assert.Contains("FastSql.AutoMapper.Extensions", findings[0].Message);
    }

    [Theory]
    [InlineData("using System.Text;")]
    [InlineData("using Microsoft.Extensions.Logging;")]
    [InlineData("global using Xunit;")]
    [InlineData("using var stream = File.OpenRead(path);")]
    public void KnownNamespace_IsAccepted(string line)
    {
        Assert.Empty(Run(new UnknownNamespaceRule(), line));
    }

    [Fact]
    public void UnknownNamespace_AllowListIsConfigurable()
    {
        var rule = new UnknownNamespaceRule(new[] { "System", "Cegi" });
        Assert.Empty(Run(rule, "using Cegi.Next.Socle;"));
    }

    // ---------- AICG009 : cryptographie ----------

    [Theory]
    [InlineData("using var md5 = MD5.Create();")]
    [InlineData("var h = SHA1.HashData(bytes);")]
    [InlineData("var p = new MD5CryptoServiceProvider();")]
    public void WeakCrypto_IsDetected(string line)
    {
        Assert.Single(Run(new WeakCryptographyRule(), line));
    }

    [Fact]
    public void StrongCrypto_IsAccepted()
    {
        Assert.Empty(Run(new WeakCryptographyRule(), "var h = SHA256.HashData(bytes);"));
    }

    // ---------- AICG010 : sorties de débogage ----------

    [Fact]
    public void ConsoleWriteLine_IsDetected()
    {
        Assert.Single(Run(new DebugOutputRule(), "Console.WriteLine(ex.Message);"));
    }
}
