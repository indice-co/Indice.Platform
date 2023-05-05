using System.Diagnostics;
using Indice.AspNetCore.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Error page</summary>
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
[SecurityHeaders]
[IdentityUI(typeof(Error40XModel))]
public abstract class BaseError40XModel : PageModel
{
    private readonly ILogger<BaseError40XModel> _logger;
    /// <summary>Will propagate to body class</summary>
    [ViewData]
    public string BodyCssClass { get; set; } = "identity-page error-page";

    /// <summary>Constructor</summary>
    /// <param name="logger"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseError40XModel(
        ILogger<BaseError40XModel> logger) {
        _logger = logger;
    }

    /// <summary>The request id</summary>
    public string? RequestId { get; set; }
    /// <summary>Should show the request id</summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    /// <summary>Status code</summary>
    public int? ErrorStatusCode { get; set; }

    /// <summary>Render the page</summary>
    public virtual IActionResult OnGet(int? statusCode) {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        ErrorStatusCode = statusCode;
        return Page();
    }

    /// <summary>Error messages map</summary>
    public Dictionary<int, Error40XMessage> ErrorMessages =>
        new () {
         [400] = new ("Bad Request", "You request does not seem to be valid."),
         [401] = new ("Unauthorized", "You do not have permission to access this resource." ),
         [403] = new ("Forbidden", "You do not have permission to access this resource." ),
         [404] = new ("Not Found", "We could not find the page you were looking for."),
    };
}

internal class Error40XModel : BaseError40XModel
{
    public Error40XModel(
        ILogger<BaseError40XModel> logger
    ) : base(logger) {
    }
}

/// <summary>
/// Represents an 40X error message
/// </summary>
/// <param name="Title"></param>
/// <param name="Message"></param>
public record Error40XMessage(string Title, string Message);
