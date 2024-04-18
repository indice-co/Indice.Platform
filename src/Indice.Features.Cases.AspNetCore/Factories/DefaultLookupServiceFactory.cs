using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Services.NoOpServices;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Cases.Factories;

/// <summary>Default lookup service factory.</summary>
/// <remarks>Constructs the factory given all the available implementations of the <see cref="ILookupService"/>.</remarks>
/// <param name="serviceProvider"></param>
public class DefaultLookupServiceFactory(IServiceProvider serviceProvider) : ILookupServiceFactory
{
    private IServiceProvider _serviceProvider { get; } = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    /// <inheritdoc />
    public ILookupService Create(string name) {
        return _serviceProvider.GetKeyedService<ILookupService>(name) ?? new NoOpLookupService();
    }
}
