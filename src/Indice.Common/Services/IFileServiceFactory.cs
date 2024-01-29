using Microsoft.Extensions.DependencyInjection;

namespace Indice.Services;

/// <summary>Factory for <see cref="IFileService"/>.</summary>
public interface IFileServiceFactory
{
    /// <summary>
    /// Create the <see cref="IFileService"/> given a name. 
    /// If an available implementation maches the name registration it will spin up an instance with its correlated configuration.
    /// In case there is no match a not <see cref="NotSupportedException"/> will be thrown.
    /// </summary>
    /// <param name="name">The name of the configuration</param>
    /// <returns>The <see cref="IFileService"/> for the requested channel.</returns>
    /// <exception cref="NotSupportedException"></exception>
    IFileService Create(string name);
}

/// <summary>Extension methods on the <see cref="IFileServiceFactory"/>.</summary>
public static class IFileServiceFactoryExtensions
{
    /// <summary>
    /// Create the <see cref="IFileService"/> default implementation. 
    /// In case there is no match a not <see cref="NotSupportedException"/> will be thrown.
    /// </summary>
    /// <returns>The <see cref="IFileService"/> for the requested channel.</returns>
    /// <exception cref="NotSupportedException"></exception>
    public static IFileService Create(this IFileServiceFactory factory) => factory.Create(null);
}


/// <summary>Default Service factory. Makes use of keyed services</summary>
public class DefaultFileServiceFactory : IFileServiceFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="serviceProvider"></param>
    public DefaultFileServiceFactory(IServiceProvider serviceProvider) {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public IFileService Create(string name) => name is null ? 
                                               _serviceProvider.GetRequiredService<IFileService>() : _serviceProvider.GetKeyedService<IFileService>(name);
}
