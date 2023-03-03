using System.Security.Claims;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Validation;

namespace Indice.Features.Identity.Core.Scopes;

/// <summary>Parsed scopes notifications decorator for <see cref="IConsentService"/>.</summary>
/// <typeparam name="TConsentService">The type of <see cref="IConsentService"/> to decorate.</typeparam>
public class ParsedScopeConsentService<TConsentService> : IConsentService where TConsentService : IConsentService
{
    private readonly TConsentService _inner;
    private readonly IParsedScopeNotificationService _parsedScopeNotificationService;

    /// <summary>Creates a new instance of <see cref="ParsedScopeConsentService{T}"/>.</summary>
    /// <param name="inner">Service to retrieve and update consent.</param>
    /// <param name="parsedScopeNotificationService">Contains methods to notify an API when a dynamic consent is altered.</param>
    public ParsedScopeConsentService(TConsentService inner, IParsedScopeNotificationService parsedScopeNotificationService) {
        _inner = inner;
        _parsedScopeNotificationService = parsedScopeNotificationService ?? throw new ArgumentNullException(nameof(parsedScopeNotificationService));
    }

    /// <summary>Checks if consent is required.</summary>
    /// <param name="subject">The user.</param>
    /// <param name="client">The client.</param>
    /// <param name="parsedScopes">The scopes.</param>
    /// <returns>Boolean if consent is required.</returns>
    public Task<bool> RequiresConsentAsync(ClaimsPrincipal subject, Client client, IEnumerable<ParsedScopeValue> parsedScopes) => _inner.RequiresConsentAsync(subject, client, parsedScopes);


    /// <summary>Updates the consent.</summary>
    /// <param name="subject">The user.</param>
    /// <param name="client">The client.</param>
    /// <param name="parsedScopes">The scopes.</param>
    public async Task UpdateConsentAsync(ClaimsPrincipal subject, Client client, IEnumerable<ParsedScopeValue> parsedScopes) {
        await _inner.UpdateConsentAsync(subject, client, parsedScopes);
        if (client.AllowRememberConsent && parsedScopes != null && parsedScopes.Any()) {
            await _parsedScopeNotificationService.Notify(client.ClientId, parsedScopes, ParsedScopeNotificationType.ConsentGranted);
        }
    }
}
