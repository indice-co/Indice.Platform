using Indice.Features.Cases.Interfaces;

namespace Indice.Features.Cases.Factories
{
    /// <summary>Factory for <see cref="ILookupService"/>.</summary>
    public interface ILookupServiceFactory
    {
        /// <summary>Create the <see cref="ILookupService"/> given the lookup service name. If an available implementation maches the given dname then an instance will be returned else an <see cref="NotSupportedException"/> will be thrown.</summary>
        /// <param name="name">The name of the lookup service.</param>
        /// <returns>The <see cref="ILookupService"/> for the given lookup service name</returns>
        /// <exception cref="NotSupportedException"></exception>
        ILookupService Create(string name);
    }
}
