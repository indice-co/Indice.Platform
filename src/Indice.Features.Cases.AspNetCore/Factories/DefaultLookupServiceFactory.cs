using Indice.Features.Cases.Interfaces;

namespace Indice.Features.Cases.Factories
{
    public class DefaultLookupServiceFactory : ILookupServiceFactory
    {
        /// <summary>Constructs the factory given all the available implementations of the <see cref="ILookupService"/>.</summary>
        /// <param name="services">The available implementations</param>
        public DefaultLookupServiceFactory(IEnumerable<ILookupService> services) {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <summary>Available <see cref="ILookupService"/> implementations.</summary>
        protected IEnumerable<ILookupService> Services { get; }

        /// <inheritdoc />
        public ILookupService Create(string name) {
            foreach (var service in Services) {
                if (service.Name.Equals(name)) {
                    return service;
                }
            }
            throw new NotSupportedException(name);
        }
    }
}
