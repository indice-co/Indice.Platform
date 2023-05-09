using Indice.AspNetCore.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for error screen.</summary>
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
[SecurityHeaders]
[IdentityUI(typeof(Error40XModel))]
public abstract class BaseError40XModel : PageModel
{
    private readonly ILogger<BaseError40XModel> _logger;

    /// <summary>Creates a new instance of <see cref="BaseError40XModel"/> class.</summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseError40XModel(ILogger<BaseError40XModel> logger) {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Will propagate to body class.</summary>
    [ViewData]
    public string BodyCssClass { get; set; } = "identity-page error-page";

    /// <summary>The request id.</summary>
    public string? RequestId { get; set; }
    /// <summary>Should show the request id.</summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    
    /// <summary>Error messages to description mapping.</summary>
    public Dictionary<int, Error40XMessage> ErrorMessages => new() {
        [400] = new("Bad Request", "You request does not seem to be valid."),
        [401] = new("Unauthorized", "You do not have permission to access this resource."),
        [403] = new("Forbidden", "You do not have permission to access this resource."),
        [404] = new("Not Found", "We could not find the page you were looking for.")
    };

    /// <summary>Status code.</summary>
    public int? ErrorStatusCode { get; set; }

    /// <summary>Error page GET handler.</summary>
    public virtual async Task<IActionResult> OnGetAsync(int? statusCode) {
        await Task.CompletedTask;
        ErrorStatusCode = statusCode;
        return Page();
    }
}

internal class Error40XModel : BaseError40XModel
{
    public Error40XModel(ILogger<Error40XModel> logger) : base(logger) { }
}

/// <summary>Represents an 40X error message.</summary>
/// <param name="Title">Error title.</param>
/// <param name="Message">Error message.</param>
public record Error40XMessage(string Title, string Message);
