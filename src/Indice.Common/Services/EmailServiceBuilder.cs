using Microsoft.Extensions.DependencyInjection;

namespace Indice.Services
{
    /// <summary>
    /// Builder class for <see cref="IEmailService"/>.
    /// </summary>
    public class EmailServiceBuilder
    {
        /// <summary>
        /// Creates a new instance of <see cref="EmailServiceBuilder"/>.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public EmailServiceBuilder(IServiceCollection services) {
            Services = services;
        }

        /// <summary>
        /// Specifies the contract for a collection of service descriptors.
        /// </summary>
        public IServiceCollection Services { get; }
    }
}
