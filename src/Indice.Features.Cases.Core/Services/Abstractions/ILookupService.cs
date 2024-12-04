using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.NoOpServices;
using Indice.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Cases.Core.Services.Abstractions;

/// <summary>Lookup service Interface.</summary>
public interface ILookupService
{
    /// <summary>Get the <see cref="ResultSet{T}"/> of a <see cref="LookupItem"/> for a specific lookup name.</summary>
    /// <param name="options">Any options to filter the lookup results.</param>
    /// <returns></returns>
    Task<ResultSet<LookupItem>> Get(ListOptions<LookupFilter>? options = null);
}

/// <summary>Factory for <see cref="ILookupService"/>.</summary>
public interface ILookupServiceFactory
{
    /// <summary>Create the <see cref="ILookupService"/> given the lookup service name.</summary>
    /// <param name="name">The name of the lookup service.</param>
    /// <returns>The <see cref="ILookupService"/> for the given lookup service name</returns>
    ILookupService Create(string name);
}

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