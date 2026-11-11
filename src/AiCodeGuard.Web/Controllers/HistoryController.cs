using AiCodeGuard.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiCodeGuard.Web.Controllers;

public class HistoryController : Controller
{
    private readonly AnalysisService _analysis;

    public HistoryController(AnalysisService analysis)
    {
        _analysis = analysis;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var history = await _analysis.GetHistoryAsync(100, cancellationToken);
        return View(history);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var result = await _analysis.GetAsync(id, cancellationToken);
        if (result is null) return NotFound();

        ViewBag.Source = _analysis.GetSource(id) ?? string.Empty;
        return View(result);
    }
}
