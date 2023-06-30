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
    /// <param name="collection"></param>
    /// <param name="scanAssemblies"></param>
    /// <returns></returns>
    public static IServiceCollection AddEndpointParameterFluentValidation(this IServiceCollection collection, params Assembly[] scanAssemblies)
        => AddEndpointParameterFluentValidation(collection, ServiceLifetime.Transient, scanAssemblies);

    /// <summary>Adds support for fluent validation validators in Minimal APIs.</summary>
    /// <param name="collection"></param>
    /// <param name="lifetime"></param>
    /// <param name="scanAssemblies"></param>
    /// <returns></returns>
    public static IServiceCollection AddEndpointParameterFluentValidation(this IServiceCollection collection, ServiceLifetime lifetime, params Assembly[] scanAssemblies) {
        collection.TryAddSingleton<IEndpointParameterValidator, EndpointParameterFluentValidator>();
        if (scanAssemblies?.Length > 0) {
            AssemblyScanner.FindValidatorsInAssemblies(scanAssemblies)
                           .ForEach(x => collection.TryAdd(ServiceDescriptor.Describe(x.InterfaceType, x.ValidatorType, lifetime)));
        }
        return collection;
    }
}
#endif