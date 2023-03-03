using System;
using System.Collections.Generic;

namespace Indice.Services;

/// <summary>Factory for <see cref="ISmsService"/>.</summary>
public interface ISmsServiceFactory
{
    /// <summary>Create the <see cref="ISmsService"/> given the requested delivery channel. If an available implementation maches the given delivery Channel then an instance will be be returned else an <see cref="NotSupportedException"/> will be thrown.</summary>
    /// <param name="channel">The name of channel.</param>
    /// <returns>The <see cref="ISmsService"/> for the requested channel</returns>
    /// <exception cref="NotSupportedException"></exception>
    ISmsService Create(string channel);
}

/// <summary>Default sms service factory.</summary>
public class DefaultSmsServiceFactory : ISmsServiceFactory
{
    /// <summary>Constructs the factory given all the available implementations of the <see cref="ISmsService"/>.</summary>
    /// <param name="services">The available implementations</param>
    public DefaultSmsServiceFactory(IEnumerable<ISmsService> services) {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>Available <see cref="ISmsService"/> implementations.</summary>
    protected IEnumerable<ISmsService> Services { get; }

    /// <inheritdoc />
    public ISmsService Create(string channel) {
        foreach (var service in Services) {
            if (service.Supports(channel)) {
                return service;
            }
        }
        throw new NotSupportedException(channel);
    }
}
