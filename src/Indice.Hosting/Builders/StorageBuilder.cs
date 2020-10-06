using Microsoft.Extensions.DependencyInjection;

namespace Indice.Hosting
{
    /// <summary>
    /// A helper class to configure the various storage choices for locking and queue persistence.
    /// </summary>
    public class StorageBuilder
    {
        /// <summary>
        /// Creates a new instance of <see cref="StorageBuilder"/>.
        /// </summary>
        /// <param name="services"></param>
        public StorageBuilder(IServiceCollection services) => Services = services;

        /// <summary>
        /// Specifies the contract for a collection of service descriptors.
        /// </summary>
        public IServiceCollection Services { get; }
    }
}
