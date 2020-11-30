using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4;
using IdentityServer4.EntityFramework.Interfaces;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores.Serialization;
using IdentityServer4.Validation;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Identity.Scopes
{
    /// <summary>
    /// Dynamic scopes notifications decorator for <see cref="IPersistedGrantService"/>.
    /// </summary>
    internal class DynamicScopePersistedGrantService<T> : IPersistedGrantService where T : IPersistedGrantService
    {
        private readonly IPersistedGrantService _inner;
        private readonly IPersistedGrantDbContext _persistedGrantDbContext;
        private readonly IDynamicScopeNotificationService _dynamicScopeNotificationService;
        private readonly IPersistentGrantSerializer _serializer;

        /// <summary>
        /// Creates a new instance of <see cref="DynamicScopePersistedGrantService{T}"/>.
        /// </summary>
        /// <param name="inner">Implements persisted grant logic.</param>
        /// <param name="persistedGrantDbContext">Abstraction for the operational data context.</param>
        /// <param name="dynamicScopeNotificationService">Contains methods to notify an API when a dynamic consent is altered.</param>
        /// <param name="serializer">Interface for persisted grant serialization.</param>
        public DynamicScopePersistedGrantService(T inner, IPersistedGrantDbContext persistedGrantDbContext, IDynamicScopeNotificationService dynamicScopeNotificationService, IPersistentGrantSerializer serializer) {
            _inner = inner;
            _persistedGrantDbContext = persistedGrantDbContext ?? throw new ArgumentNullException(nameof(persistedGrantDbContext));
            _dynamicScopeNotificationService = dynamicScopeNotificationService ?? throw new ArgumentNullException(nameof(dynamicScopeNotificationService));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <inheritdoc/>
        public async Task RemoveAllGrantsAsync(string subjectId, string clientId = null, string sessionId = null) {
            var grants = await _persistedGrantDbContext.PersistedGrants.Where(x => x.SubjectId == subjectId && x.ClientId == clientId).ToListAsync();
            await _inner.RemoveAllGrantsAsync(subjectId, clientId);
            var consents = grants.Where(x => x.Type == IdentityServerConstants.PersistedGrantTypes.UserConsent);
            var scopes = new List<string>();
            foreach (var consent in consents) {
                var consentData = _serializer.Deserialize<Consent>(consent.Data);
                if (consentData?.Scopes != null && consentData.Scopes.Any()) {
                    scopes.AddRange(consentData.Scopes);
                }
            }
            await _dynamicScopeNotificationService.Notify(clientId, scopes.Select(x => new ParsedScopeValue(x)), DynamicScopeNotificationType.GrantsRevoked);
        }

        /// <inheritdoc/>
        Task<IEnumerable<Grant>> IPersistedGrantService.GetAllGrantsAsync(string subjectId) => _inner.GetAllGrantsAsync(subjectId);
    }
}
