using System.Threading.Tasks;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Stores.Serialization;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Identity.Features
{
    public class DefaultTrustedDeviceAuthorizationCodeChallengeStore : DefaultGrantStore<TrustedDeviceAuthorizationCode>, ITrustedDeviceAuthorizationCodeChallengeStore
    {
        public DefaultTrustedDeviceAuthorizationCodeChallengeStore(
            IPersistedGrantStore store,
            IPersistentGrantSerializer serializer,
            IHandleGenerationService handleGenerationService,
            ILogger<DefaultTrustedDeviceAuthorizationCodeChallengeStore> logger
        ) : base("trusted_device_authorization_code", store, serializer, handleGenerationService, logger) { }

        public Task<string> Store(TrustedDeviceAuthorizationCode code) =>
            CreateItemAsync(code, code.ClientId, code.Subject.GetSubjectId(), null, code.Description, code.CreationTime, code.Lifetime);
    }
}
