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
    /// An implementation of <see cref="IConsentService"/> where a specified API is notified when a consent is updated.
    /// </summary>
    public class DynamicScopeConsentService : IConsentService
    {
        private readonly IConsentService _consentService;
        private readonly IDynamicScopeNotificationService _dynamicScopeNotificationService;

        /// <summary>
        /// Creates a new instance of <see cref="DynamicScopeConsentService"/>.
        /// </summary>
        /// <param name="consentService">Service to retrieve and update consent.</param>
        /// <param name="dynamicScopeNotificationService">Contains methods to notify an API when a dynamic consent is altered.</param>
        public DynamicScopeConsentService(IConsentService consentService, IDynamicScopeNotificationService dynamicScopeNotificationService) {
            _consentService = consentService ?? throw new ArgumentNullException(nameof(consentService));
            _dynamicScopeNotificationService = dynamicScopeNotificationService ?? throw new ArgumentNullException(nameof(dynamicScopeNotificationService));
        }

        /// <summary>
        /// Checks if consent is required.
        /// </summary>
        /// <param name="subject">The user.</param>
        /// <param name="client">The client.</param>
        /// <param name="scopes">The scopes.</param>
        /// <returns>Boolean if consent is required.</returns>
        public Task<bool> RequiresConsentAsync(ClaimsPrincipal subject, Client client, IEnumerable<string> scopes) => _consentService.RequiresConsentAsync(subject, client, scopes);

        /// <summary>
        /// Updates the consent.
        /// </summary>
        /// <param name="subject">The user.</param>
        /// <param name="client">The client.</param>
        /// <param name="scopes">The scopes.</param>
        public async Task UpdateConsentAsync(ClaimsPrincipal subject, Client client, IEnumerable<string> scopes) {
            await _consentService.UpdateConsentAsync(subject, client, scopes);
            if (client.AllowRememberConsent && scopes != null && scopes.Any()) {
                await _dynamicScopeNotificationService.Notify(client.ClientId, scopes, DynamicScopeNotificationType.ConsentGranted);
            }
        }
    }
}
