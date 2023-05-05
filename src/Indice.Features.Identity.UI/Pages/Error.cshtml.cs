using System.Diagnostics;
using IdentityServer4.Services;
using Indice.AspNetCore.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Error page</summary>
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
[SecurityHeaders]
[IdentityUI(typeof(ErrorModel))]
public abstract class BaseErrorModel : PageModel
{
    private readonly ILogger<BaseErrorModel> _logger;
    private readonly IIdentityServerInteractionService _interaction;
    /// <summary>Will propagate to body class</summary>
    [ViewData]
    public string BodyCssClass { get; set; } = "identity-page";

    /// <summary>Constructor</summary>
    /// <param name="logger"></param>
    /// <param name="interactionService"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseErrorModel(
        ILogger<BaseErrorModel> logger,
        IIdentityServerInteractionService interactionService) {
        _logger = logger;
        _interaction = interactionService ?? throw new ArgumentNullException(nameof(interactionService));
    }

    /// <summary>The request id</summary>
    public string? RequestId { get; set; }
    /// <summary>Should show the request id</summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    /// <summary>The error message</summary>
    public string? Message { get; set; }

    /// <summary>Render the page</summary>
    public async Task<IActionResult> OnGetAsync(string errorId) {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var message = await _interaction.GetErrorContextAsync(errorId);
        if (message is not null) {
            Message = message.Error;
        }
        return Page();
    }
}

internal class ErrorModel : BaseErrorModel
{
    public ErrorModel(
        ILogger<BaseErrorModel> logger, IIdentityServerInteractionService interactionService
    ) : base(logger, interactionService) {
    }
}