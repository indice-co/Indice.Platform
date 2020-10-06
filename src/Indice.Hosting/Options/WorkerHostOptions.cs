using Microsoft.Extensions.DependencyInjection;

namespace Indice.Hosting
{
    /// <summary>
    /// Options for configuring the worker host.
    /// </summary>
    public class WorkerHostOptions
    {
        /// <summary>
        /// Creates a new instance of <see cref="WorkerHostOptions"/>.
        /// </summary>
        /// <param name="services"></param>
        public WorkerHostOptions(IServiceCollection services) => Services = services;

        /// <summary>
        /// Specifies the contract for a collection of service descriptors.
        /// </summary>
        public IServiceCollection Services { get; }
    }
}
