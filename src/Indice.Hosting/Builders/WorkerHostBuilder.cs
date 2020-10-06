using Microsoft.Extensions.DependencyInjection;

namespace Indice.Hosting
{
    /// <summary>
    /// A helper class to configure the worker host.
    /// </summary>
    public class WorkerHostBuilder
    {
        /// <summary>
        /// Creates a new instance of <see cref="WorkerHostBuilder"/>.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public WorkerHostBuilder(IServiceCollection services) => Services = services;

        /// <summary>
        /// Specifies the contract for a collection of service descriptors.
        /// </summary>
        public IServiceCollection Services { get; }
    }
}
