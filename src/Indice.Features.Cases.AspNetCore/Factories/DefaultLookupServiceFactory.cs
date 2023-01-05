using Indice.Features.Cases.Interfaces;

namespace Indice.Features.Cases.Factories
{
    /// <summary>Default lookup service factory.</summary>
    public class DefaultLookupServiceFactory : ILookupServiceFactory
    {
        /// <summary>Constructs the factory given all the available implementations of the <see cref="ILookupService"/>.</summary>
        /// <param name="getLookupService"></param>
        public DefaultLookupServiceFactory(Func<string, ILookupService> getLookupService) {
            _getLookupService = getLookupService ?? throw new ArgumentNullException(nameof(getLookupService));
        }

        private Func<string, ILookupService> _getLookupService { get; }

        /// <inheritdoc />
        public ILookupService Create(string name) {
            return _getLookupService(name);
        }
    }
}
