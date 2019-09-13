using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Services;

namespace Indice.AspNetCore.Identity.Scopes
{
    /// <summary>
    /// Dynamic scopes notifications decorator for <see cref="IConsentService"/>.
    /// </summary>
    public class DynamicScopeConsentService<T> : IConsentService where T : IConsentService
    {
        private readonly T _innerConsentService;
        private readonly IDynamicScopeNotificationService _dynamicScopeNotificationService;

        /// <summary>
        /// Creates a new instance of <see cref="DynamicScopeConsentService{T}"/>.
        /// </summary>
        /// <param name="innerConsentService">Service to retrieve and update consent.</param>
        /// <param name="dynamicScopeNotificationService">Contains methods to notify an API when a dynamic consent is altered.</param>
        public DynamicScopeConsentService(T innerConsentService, IDynamicScopeNotificationService dynamicScopeNotificationService) {
            _innerConsentService = innerConsentService;
            _dynamicScopeNotificationService = dynamicScopeNotificationService ?? throw new ArgumentNullException(nameof(dynamicScopeNotificationService));
        }

        /// <summary>
        /// Checks if consent is required.
        /// </summary>
        /// <param name="subject">The user.</param>
        /// <param name="client">The client.</param>
        /// <param name="scopes">The scopes.</param>
        /// <returns>Boolean if consent is required.</returns>
        public Task<bool> RequiresConsentAsync(ClaimsPrincipal subject, Client client, IEnumerable<string> scopes) => _innerConsentService.RequiresConsentAsync(subject, client, scopes);

        /// <summary>
        /// Updates the consent.
        /// </summary>
        /// <param name="subject">The user.</param>
        /// <param name="client">The client.</param>
        /// <param name="scopes">The scopes.</param>
        public async Task UpdateConsentAsync(ClaimsPrincipal subject, Client client, IEnumerable<string> scopes) {
            await _innerConsentService.UpdateConsentAsync(subject, client, scopes);
            if (client.AllowRememberConsent && scopes != null && scopes.Any()) {
                await _dynamicScopeNotificationService.Notify(client.ClientId, scopes, DynamicScopeNotificationType.ConsentGranted);
            }
        }
    }
}
