using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Indice.AspNetCore.Filters;
using Indice.AspNetCore.Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Identity.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[Authorize]
[Route("grants")]
[SecurityHeaders]
public class GrantsController : Controller
{
    private readonly IIdentityServerInteractionService _interaction;
    private readonly IClientStore _clients;
    private readonly IResourceStore _resources;
    private readonly IEventService _events;
    /// <summary>The name of the controller.</summary>
    public const string Name = "Grants";

    public GrantsController(IIdentityServerInteractionService interaction, IClientStore clients, IResourceStore resources, IEventService events) {
        _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
        _clients = clients ?? throw new ArgumentNullException(nameof(clients));
        _resources = resources ?? throw new ArgumentNullException(nameof(resources));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    [HttpGet]
    public async Task<IActionResult> Index() => View(nameof(Index), await BuildViewModelAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Revoke(string clientId) {
        await _interaction.RevokeUserConsentAsync(clientId);
        await _events.RaiseAsync(new GrantsRevokedEvent(User.GetSubjectId(), clientId));
        return RedirectToAction(nameof(Index));
    }

    private async Task<GrantsViewModel> BuildViewModelAsync() {
        var grants = await _interaction.GetAllUserGrantsAsync();
        var list = new List<GrantViewModel>();
        foreach (var grant in grants) {
            var client = await _clients.FindClientByIdAsync(grant.ClientId);
            if (client != null) {
                var resources = await _resources.FindResourcesByScopeAsync(grant.Scopes);
                var item = new GrantViewModel {
                    ClientId = client.ClientId,
                    ClientName = client.ClientName ?? client.ClientId,
                    ClientLogoUrl = client.LogoUri,
                    ClientUrl = client.ClientUri,
                    Description = grant.Description,
                    Created = grant.CreationTime,
                    Expires = grant.Expiration,
                    IdentityGrantNames = resources.IdentityResources.Select(x => x.DisplayName ?? x.Name).ToArray(),
                    ApiGrantNames = resources.ApiScopes.Select(x => x.DisplayName ?? x.Name).ToArray()
                };
                list.Add(item);
            }
        }
        return new GrantsViewModel {
            Grants = list
        };
    }
}
