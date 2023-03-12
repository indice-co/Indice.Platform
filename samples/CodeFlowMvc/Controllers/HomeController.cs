using System.Diagnostics;
using CodeFlowMvc.Models;
using Microsoft.AspNetCore.Mvc;

namespace CodeFlowMvc.Controllers;

public class HomeController : Controller
{
    public ViewResult Index() => View();

    public ViewResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public ViewResult Error() => View(new ErrorViewModel {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
    });
}
