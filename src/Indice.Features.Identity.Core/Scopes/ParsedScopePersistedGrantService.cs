using IdentityServer4;
using IdentityServer4.EntityFramework.Interfaces;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores.Serialization;
using IdentityServer4.Validation;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Identity.Core.Scopes
{
    /// <summary>Parsed scopes notifications decorator for <see cref="IPersistedGrantService"/>.</summary>
    /// <typeparam name="TPersistedGrantService">The type of <see cref="IPersistedGrantService"/> to decorate.</typeparam>
    public class ParsedScopePersistedGrantService<TPersistedGrantService> : IPersistedGrantService where TPersistedGrantService : IPersistedGrantService
    {
        private readonly IPersistedGrantService _inner;
        private readonly IPersistedGrantDbContext _persistedGrantDbContext;
        private readonly IParsedScopeNotificationService _parsedScopeNotificationService;
        private readonly IPersistentGrantSerializer _serializer;
        private readonly IScopeParser _scopeParser;

        /// <summary>Creates a new instance of <see cref="ParsedScopePersistedGrantService{T}"/>.</summary>
        /// <param name="inner">Implements persisted grant logic.</param>
        /// <param name="persistedGrantDbContext">Abstraction for the operational data context.</param>
        /// <param name="parsedScopeNotificationService">Contains methods to notify an API when a dynamic consent is altered.</param>
        /// <param name="serializer">Interface for persisted grant serialization.</param>
        /// <param name="scopeParser">Allows parsing raw scopes values into structured scope values.</param>
        public ParsedScopePersistedGrantService(TPersistedGrantService inner, IPersistedGrantDbContext persistedGrantDbContext, IParsedScopeNotificationService parsedScopeNotificationService, IPersistentGrantSerializer serializer,
            IScopeParser scopeParser) {
            _inner = inner;
            _persistedGrantDbContext = persistedGrantDbContext ?? throw new ArgumentNullException(nameof(persistedGrantDbContext));
            _parsedScopeNotificationService = parsedScopeNotificationService ?? throw new ArgumentNullException(nameof(parsedScopeNotificationService));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _scopeParser = scopeParser ?? throw new ArgumentNullException(nameof(scopeParser));
        }

        /// <inheritdoc/>
        public async Task RemoveAllGrantsAsync(string subjectId, string clientId = null, string sessionId = null) {
            var removedGrants = await _persistedGrantDbContext.PersistedGrants.Where(x => x.SubjectId == subjectId && x.ClientId == clientId).ToListAsync();
            await _inner.RemoveAllGrantsAsync(subjectId, clientId);
            var consents = removedGrants.Where(x => x.Type == IdentityServerConstants.PersistedGrantTypes.UserConsent);
            var scopeValues = new List<string>();
            foreach (var consent in consents) {
                var consentData = _serializer.Deserialize<Consent>(consent.Data);
                if (consentData?.Scopes != null && consentData.Scopes.Any()) {
                    scopeValues.AddRange(consentData.Scopes);
                }
            }
            var parsedScopesResult = _scopeParser.ParseScopeValues(scopeValues);
            if (!parsedScopesResult.Succeeded) {
                return;
            }
            await _parsedScopeNotificationService.Notify(clientId, parsedScopesResult.ParsedScopes, ParsedScopeNotificationType.GrantsRevoked);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<Grant>> GetAllGrantsAsync(string subjectId) => _inner.GetAllGrantsAsync(subjectId);
    }
}
