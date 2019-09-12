using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4;
using IdentityServer4.EntityFramework.Interfaces;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Indice.AspNetCore.Identity.Scopes
{
    /// <summary>
    /// An implementation of <see cref="IPersistedGrantService"/> where a specified API is notified when a grant is revoked.
    /// </summary>
    public class DynamicScopePersistedGrantService : IPersistedGrantService
    {
        private readonly IPersistedGrantService _persistedGrantService;
        private readonly IPersistedGrantDbContext _persistedGrantDbContext;
        private readonly IDynamicScopeNotificationService _dynamicScopeNotificationService;
        private readonly ILogger<DynamicScopePersistedGrantService> _logger;
        private readonly IPersistentGrantSerializer _serializer;

        /// <summary>
        /// Creates a new instance of <see cref="DynamicScopePersistedGrantService"/>.
        /// </summary>
        /// <param name="persistedGrantService">Implements persisted grant logic.</param>
        /// <param name="persistedGrantDbContext">Abstraction for the operational data context.</param>
        /// <param name="dynamicScopeNotificationService">Contains methods to notify an API when a dynamic consent is altered.</param>
        /// <param name="logger">Represents a type used to perform logging.</param>
        /// <param name="serializer">Interface for persisted grant serialization.</param>
        public DynamicScopePersistedGrantService(IPersistedGrantService persistedGrantService, IPersistedGrantDbContext persistedGrantDbContext, IDynamicScopeNotificationService dynamicScopeNotificationService, ILogger<DynamicScopePersistedGrantService> logger,
            IPersistentGrantSerializer serializer) {
            _persistedGrantService = persistedGrantService ?? throw new ArgumentNullException(nameof(persistedGrantService));
            _persistedGrantDbContext = persistedGrantDbContext ?? throw new ArgumentNullException(nameof(persistedGrantDbContext));
            _dynamicScopeNotificationService = dynamicScopeNotificationService ?? throw new ArgumentNullException(nameof(dynamicScopeNotificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <summary>
        /// Gets all grants for a given subject Id.
        /// </summary>
        /// <param name="subjectId">The subject identifier.</param>
        public Task<IEnumerable<Consent>> GetAllGrantsAsync(string subjectId) => _persistedGrantService.GetAllGrantsAsync(subjectId);

        /// <summary>
        /// Removes all grants for a given subject id and client id combination.
        /// </summary>
        /// <param name="subjectId">The subject identifier.</param>
        /// <param name="clientId">The client identifier.</param>
        public async Task RemoveAllGrantsAsync(string subjectId, string clientId) {
            var grants = await _persistedGrantDbContext.PersistedGrants.Where(x => x.SubjectId == subjectId && x.ClientId == clientId).ToListAsync();
            await _persistedGrantService.RemoveAllGrantsAsync(subjectId, clientId);
            var consents = grants.Where(x => x.Type == IdentityServerConstants.PersistedGrantTypes.UserConsent);
            var scopes = new List<string>();
            foreach (var consent in consents) {
                try {
                    var consentData = _serializer.Deserialize<Consent>(consent.Data);
                    if (consentData?.Scopes != null && consentData.Scopes.Any()) {
                        scopes.AddRange(consentData.Scopes);
                    }
                } catch (JsonSerializationException) {
                    _logger.LogInformation($"Could not deserialize consent with value: {consent.Data}.");
                }
            }
            await _dynamicScopeNotificationService.Notify(clientId, scopes, DynamicScopeNotificationType.GrantsRevoked);
        }
    }
}
