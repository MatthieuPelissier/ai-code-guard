using System.Diagnostics;
using AiCodeGuard.Web.Models;
using AiCodeGuard.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiCodeGuard.Web.Controllers;

public class HomeController : Controller
{
    private readonly AnalysisService _analysis;

    public HomeController(AnalysisService analysis)
    {
        _analysis = analysis;
    }

    [HttpGet]
    public IActionResult Index()
    {
        ViewBag.LlmAvailable = _analysis.LlmAvailable;
        return View();
    }

    /// <summary>Point d'entrée AJAX (jQuery) : analyse le code et renvoie le rapport en JSON.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Analyze([FromBody] AnalyzeRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(new { errors });
        }

        var result = await _analysis.AnalyzeAsync(request, cancellationToken);
        return Json(result);
    }

    [HttpGet]
    public IActionResult Error()
    {
        ViewBag.RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        return View();
    }
}
