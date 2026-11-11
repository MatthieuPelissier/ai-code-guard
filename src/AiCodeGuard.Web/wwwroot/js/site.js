/* AI Code Guard — interactions côté client (jQuery). */
(function ($) {
    "use strict";

    var samples = {
        bad: { name: "UserRepository.cs", code: "using System.Data.SqlClient;\nusing System.Security.Cryptography;\nusing System.Text;\nusing FastSql.AutoMapper.Extensions;\n\nnamespace Demo.Data;\n\n// Exemple réaliste de code produit par un assistant IA en réponse à :\n// « Écris-moi un repository C# pour gérer les utilisateurs avec SQL Server »\npublic class UserRepository\n{\n    private const string ConnectionString = \"Server=prod-sql01;Database=Crm;User Id=sa;Password=Admin2024!;\";\n\n    public User GetUser(string login)\n    {\n        using var connection = new SqlConnection(ConnectionString);\n        connection.Open();\n        var command = new SqlCommand($\"SELECT * FROM Users WHERE Login = '{login}'\", connection);\n        using var reader = command.ExecuteReader();\n        // ... reste du code pour mapper le résultat\n        return reader.MapTo<User>();\n    }\n\n    public string HashPassword(string password)\n    {\n        using var md5 = MD5.Create();\n        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(password));\n        return Convert.ToBase64String(bytes);\n    }\n\n    public async void SaveAuditAsync(User user)\n    {\n        try\n        {\n            await AuditClient.SendAsync(user);\n        }\n        catch { }\n    }\n\n    public int CountActiveUsers()\n    {\n        try\n        {\n            return CountAsync(\"Active = 1\").Result;\n        }\n        catch (Exception ex)\n        {\n            Console.WriteLine(ex.Message);\n            return 0;\n        }\n    }\n\n    private Task<int> CountAsync(string filter)\n    {\n        throw new NotImplementedException();\n    }\n}\n" },
        good: { name: "OrderService.cs", code: "using Microsoft.Extensions.Logging;\n\nnamespace Demo.Orders;\n\npublic interface IOrderRepository\n{\n    Task<Order?> FindAsync(int orderId, CancellationToken cancellationToken);\n    Task SaveAsync(Order order, CancellationToken cancellationToken);\n}\n\npublic sealed record Order(int Id, decimal Amount, bool IsPaid);\n\n/// <summary>Service métier : version relue et corrigée d'un code initialement généré par IA.</summary>\npublic sealed class OrderService\n{\n    private readonly IOrderRepository _repository;\n    private readonly ILogger<OrderService> _logger;\n\n    public OrderService(IOrderRepository repository, ILogger<OrderService> logger)\n    {\n        _repository = repository ?? throw new ArgumentNullException(nameof(repository));\n        _logger = logger ?? throw new ArgumentNullException(nameof(logger));\n    }\n\n    public async Task<bool> MarkAsPaidAsync(int orderId, CancellationToken cancellationToken)\n    {\n        if (orderId <= 0)\n        {\n            throw new ArgumentOutOfRangeException(nameof(orderId), \"L'identifiant doit être positif.\");\n        }\n\n        var order = await _repository.FindAsync(orderId, cancellationToken);\n        if (order is null)\n        {\n            _logger.LogWarning(\"Commande {OrderId} introuvable\", orderId);\n            return false;\n        }\n\n        if (order.IsPaid)\n        {\n            return true;\n        }\n\n        await _repository.SaveAsync(order with { IsPaid = true }, cancellationToken);\n        _logger.LogInformation(\"Commande {OrderId} marquée comme payée\", orderId);\n        return true;\n    }\n}\n" }
    };

    function escapeHtml(value) {
        return $("<div>").text(value == null ? "" : String(value)).html();
    }

    function verdictClass(verdict) {
        return "verdict-" + String(verdict || "").toLowerCase();
    }

    function renderResult(result) {
        $("#result-empty, #error").attr("hidden", true);
        $("#result").removeAttr("hidden");

        $("#score").text(result.score);
        $("#verdict")
            .removeClass("verdict-accepted verdict-needsreview verdict-rejected")
            .addClass(verdictClass(result.verdict))
            .text(result.verdictLabel);
        $("#summary").text(
            result.fileName + " · " + result.lineCount + " lignes · " +
            result.findings.length + " problème(s) détecté(s)");

        var $list = $("#findings").empty();
        if (result.findings.length === 0) {
            $list.append('<li class="finding ok">Aucun problème détecté. Une revue humaine reste nécessaire avant fusion.</li>');
        }
        $.each(result.findings, function (_, f) {
            $list.append(
                '<li class="finding sev-' + escapeHtml(f.severity.toLowerCase()) + '">' +
                    '<div class="finding-head">' +
                        '<span class="pill">' + escapeHtml(f.ruleId) + '</span> ' +
                        '<strong>' + escapeHtml(f.title) + '</strong> ' +
                        '<span class="muted">ligne ' + f.line + ' · ' + escapeHtml(f.severityLabel) + '</span>' +
                    '</div>' +
                    '<div>' + escapeHtml(f.message) + '</div>' +
                    '<code class="snippet">' + escapeHtml(f.snippet) + '</code>' +
                    '<div class="reco">→ ' + escapeHtml(f.recommendation) + '</div>' +
                '</li>');
        });

        if (result.llmReview) {
            $("#llm-text").text(result.llmReview);
            $("#llm-review").removeAttr("hidden");
        } else {
            $("#llm-review").attr("hidden", true);
        }

        $("#details-link").attr("href", "/History/Details/" + result.id);
    }

    function renderError(message) {
        $("#result-empty, #result").attr("hidden", true);
        $("#error").removeAttr("hidden").text(message);
    }

    $(function () {
        $("[data-sample]").on("click", function () {
            var sample = samples[$(this).data("sample")];
            $("#fileName").val(sample.name);
            $("#code").val(sample.code).trigger("focus");
        });

        // Tabulation dans la zone de code : insère 4 espaces au lieu de quitter le champ.
        $("#code").on("keydown", function (e) {
            if (e.key !== "Tab") return;
            e.preventDefault();
            var el = this, start = el.selectionStart, end = el.selectionEnd;
            el.value = el.value.substring(0, start) + "    " + el.value.substring(end);
            el.selectionStart = el.selectionEnd = start + 4;
        });

        $("#analyze-form").on("submit", function (e) {
            e.preventDefault();

            var code = $("#code").val();
            if (!$.trim(code)) {
                renderError("Collez du code avant de lancer l'analyse.");
                return;
            }

            var $btn = $("#analyze-btn").prop("disabled", true).text("Analyse…");

            $.ajax({
                url: "/Home/Analyze",
                method: "POST",
                contentType: "application/json; charset=utf-8",
                headers: {
                    RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
                },
                data: JSON.stringify({
                    fileName: $("#fileName").val(),
                    code: code,
                    useLlm: $("#useLlm").is(":checked")
                })
            })
                .done(renderResult)
                .fail(function (xhr) {
                    var errors = xhr.responseJSON && xhr.responseJSON.errors;
                    renderError(errors ? [].concat(errors).join(" ") : "Erreur lors de l'analyse (HTTP " + xhr.status + ").");
                })
                .always(function () {
                    $btn.prop("disabled", false).text("Analyser");
                });
        });
    });
})(jQuery);
