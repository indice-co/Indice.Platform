using System.Diagnostics;
using IdentityServer4.Services;
using Indice.AspNetCore.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for error screen.</summary>
[IdentityUI(typeof(ErrorModel))]
[IgnoreAntiforgeryToken]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[SecurityHeaders]
public abstract class BaseErrorModel : PageModel
{
    private readonly ILogger<BaseErrorModel> _logger;
    private readonly IIdentityServerInteractionService _interaction;

    /// <summary>Creates a new instance of <see cref="BaseErrorModel"/> class.</summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <param name="interactionService">Provide services be used by the user interface to communicate with IdentityServer.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseErrorModel(
        ILogger<BaseErrorModel> logger,
        IIdentityServerInteractionService interactionService
    ) {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _interaction = interactionService ?? throw new ArgumentNullException(nameof(interactionService));
    }

    /// <summary>Will propagate to body class.</summary>
    [ViewData]
    public string BodyCssClass { get; set; } = "identity-page error-page";

    /// <summary>The request id.</summary>
    public string? RequestId { get; set; }
    /// <summary>Should show the request id.</summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    /// <summary>The error message.</summary>
    public string? Message { get; set; }

    /// <summary>Error page GET handler.</summary>
    public virtual async Task<IActionResult> OnGetAsync(string errorId) {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        if (!string.IsNullOrEmpty(errorId)) {
            Message = (await _interaction.GetErrorContextAsync(errorId))?.ErrorDescription;
        }
        return Page();
    }
}

internal class ErrorModel : BaseErrorModel
{
    public ErrorModel(
        ILogger<BaseErrorModel> logger,
        IIdentityServerInteractionService interactionService
    ) : base(logger, interactionService) { }
}