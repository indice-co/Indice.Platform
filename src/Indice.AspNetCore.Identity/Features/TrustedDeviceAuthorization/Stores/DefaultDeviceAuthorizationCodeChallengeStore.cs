using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Stores.Serialization;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// 
    /// </summary>
    public class DefaultDeviceAuthorizationCodeChallengeStore : DefaultGrantStore<TrustedDeviceAuthorizationCode>, IDeviceAuthorizationCodeChallengeStore
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="grantType"></param>
        /// <param name="store"></param>
        /// <param name="serializer"></param>
        /// <param name="handleGenerationService"></param>
        /// <param name="logger"></param>
        protected DefaultDeviceAuthorizationCodeChallengeStore(
            string grantType,
            IPersistedGrantStore store,
            IPersistentGrantSerializer serializer,
            IHandleGenerationService handleGenerationService, 
            ILogger logger
        ) : base(grantType, store, serializer, handleGenerationService, logger) {
        }
    }
}
