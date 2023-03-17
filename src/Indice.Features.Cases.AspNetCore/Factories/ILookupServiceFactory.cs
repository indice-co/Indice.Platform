using Indice.Features.Cases.Interfaces;

namespace Indice.Features.Cases.Factories;

/// <summary>Factory for <see cref="ILookupService"/>.</summary>
public interface ILookupServiceFactory
{
    /// <summary>Create the <see cref="ILookupService"/> given the lookup service name.</summary>
    /// <param name="name">The name of the lookup service.</param>
    /// <returns>The <see cref="ILookupService"/> for the given lookup service name</returns>
    ILookupService Create(string name);
}
