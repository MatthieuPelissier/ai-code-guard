using AiCodeGuard.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiCodeGuard.Web.Controllers;

/// <summary>Affiche le référentiel de règles : la « charte » appliquée automatiquement.</summary>
public class RulesController : Controller
{
    private readonly AnalysisService _analysis;

    public RulesController(AnalysisService analysis)
    {
        _analysis = analysis;
    }

    [HttpGet]
    public IActionResult Index() => View(_analysis.GetRules());
}
