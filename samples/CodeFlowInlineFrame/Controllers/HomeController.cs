using System.Diagnostics;
using CodeFlowInlineFrame.Models;
using Microsoft.AspNetCore.Mvc;

namespace CodeFlowInlineFrame.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    public const string Name = "Home";

    public HomeController(ILogger<HomeController> logger) {
        _logger = logger;
    }

    public IActionResult Index() {
        return View();
    }

    public IActionResult Privacy() {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpGet("error")]
    public IActionResult Error() {
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        _logger.LogError($"An error occured: {requestId}");
        return View(new ErrorViewModel {
            RequestId = requestId
        });
    }
}
