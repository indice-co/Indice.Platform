using Indice.Features.Cases.Interfaces;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Used in order to register lookup service implementations, factories &amp; instances.
    /// </summary>
    public static class LookupServiceExtensions
    {
        /// <summary>
        /// Use this to register <typeparamref name="TLookupService"/>.
        /// </summary>
        /// <typeparam name="TLookupService"></typeparam>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="key"></param>
        public static void AddLookupService<TLookupService>(this IServiceCollection services, string key)
            where TLookupService : class, ILookupService {
            services.AddKeyedService<ILookupService, TLookupService, string>(key, ServiceLifetime.Transient);
        }
    }
}
