#if NET7_0_OR_GREATER
using System.Reflection;
using FluentValidation;
using Indice.AspNetCore.Http.Validation;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary><see cref="IServiceCollection"/> extensions for configuring <see cref="IEndpointParameterValidator" />.</summary>
public static class EndpointParameterValidationConfiguration
{
    /// <summary>Adds support for fluent validation validators in Minimal APIs.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="scanAssemblies">The assemblies to scan.</param>
    public static IServiceCollection AddEndpointParameterFluentValidation(this IServiceCollection services, params Assembly[] scanAssemblies)
        => AddEndpointParameterFluentValidation(services, ServiceLifetime.Transient, scanAssemblies);

    /// <summary>Adds support for fluent validation validators in Minimal APIs.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="lifetime">Specifies the lifetime of a service in an <see cref="IServiceCollection"/>.</param>
    /// <param name="scanAssemblies">The assemblies to scan.</param>
    public static IServiceCollection AddEndpointParameterFluentValidation(this IServiceCollection services, ServiceLifetime lifetime, params Assembly[] scanAssemblies) {
        services.TryAddSingleton<IEndpointParameterValidator, EndpointParameterFluentValidator>();
        if (scanAssemblies?.Length > 0) {
            AssemblyScanner.FindValidatorsInAssemblies(scanAssemblies)
                           .ForEach(x => services.TryAdd(ServiceDescriptor.Describe(x.InterfaceType, x.ValidatorType, lifetime)));
        }
        return services;
    }
}
#endif