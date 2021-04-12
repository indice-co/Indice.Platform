using System.Threading.Tasks;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Stores.Serialization;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// The default implementation of the <see cref="IAuthorizationCodeChallengeStore"/>, taking advantage of the underlying <see cref="IPersistedGrantStore"/> store.
    /// </summary>
    internal class DefaultAuthorizationCodeChallengeStore : DefaultGrantStore<AuthorizationCode>, IAuthorizationCodeChallengeStore
    {
        /// <summary>
        /// Creates a new instance of <see cref="DefaultAuthorizationCodeChallengeStore"/>.
        /// </summary>
        /// <param name="store">Interface for persisting any type of grant.</param>
        /// <param name="serializer">Interface for persisted grant serialization.</param>
        /// <param name="handleGenerationService">Interface for the handle generation service.</param>
        /// <param name="logger">Represents a type used to perform logging.</param>
        public DefaultAuthorizationCodeChallengeStore(
            IPersistedGrantStore store,
            IPersistentGrantSerializer serializer,
            IHandleGenerationService handleGenerationService,
            ILogger<DefaultAuthorizationCodeChallengeStore> logger
        ) : base("trusted_device_authorization_code", store, serializer, handleGenerationService, logger) { }

        /// <inheritdoc />
        public Task<string> Create(AuthorizationCode code) => CreateItemAsync(code, code.ClientId, code.Subject.GetSubjectId(), null, code.Description, code.CreationTime, code.Lifetime);
    }
}
