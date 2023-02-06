using IdentityServer4.Extensions;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Stores.Serialization;
using Indice.Features.Identity.Core.DeviceAuthentication.Models;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Identity.Core.DeviceAuthentication.Stores
{
    /// <summary>The default implementation of the <see cref="IDeviceAuthenticationCodeChallengeStore"/>, taking advantage of the underlying <see cref="IPersistedGrantStore"/> store.</summary>
    internal class DefaultDeviceAuthenticationCodeChallengeStore : DefaultGrantStore<DeviceAuthenticationCode>, IDeviceAuthenticationCodeChallengeStore
    {
        /// <summary>Creates a new instance of <see cref="DefaultDeviceAuthenticationCodeChallengeStore"/>.</summary>
        /// <param name="store">Interface for persisting any type of grant.</param>
        /// <param name="serializer">Interface for persisted grant serialization.</param>
        /// <param name="handleGenerationService">Interface for the handle generation service.</param>
        /// <param name="logger">Represents a type used to perform logging.</param>
        public DefaultDeviceAuthenticationCodeChallengeStore(
            IPersistedGrantStore store,
            IPersistentGrantSerializer serializer,
            IHandleGenerationService handleGenerationService,
            ILogger<DefaultDeviceAuthenticationCodeChallengeStore> logger
        ) : base("device_authentication_code", store, serializer, handleGenerationService, logger) { }

        /// <inheritdoc />
        public Task<string> GenerateChallenge(DeviceAuthenticationCode code) => CreateItemAsync(code, code.ClientId, code.Subject?.GetSubjectId(), null, null, code.CreationTime, code.Lifetime);

        /// <inheritdoc />
        public Task<DeviceAuthenticationCode> GetDeviceAuthenticationCode(string key) => GetItemAsync(key);

        /// <inheritdoc />
        public Task RemoveDeviceAuthenticationCode(string key) => RemoveItemAsync(key);
    }
}
