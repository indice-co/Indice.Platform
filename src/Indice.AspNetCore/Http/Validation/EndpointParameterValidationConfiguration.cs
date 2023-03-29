#if NET7_0_OR_GREATER
using System.Reflection;
using FluentValidation;
using Indice.AspNetCore.Http.Validation;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> extensions for configuring <see cref="IEndpointParameterValidator" />
/// </summary>
public static class EndpointParameterValidationConfiguration
{
    /// <summary>
    /// Adds support for fluent validation validators in Minimal apis.
    /// </summary>
    /// <param name="collection"></param>
    /// <param name="scanAssemblies"></param>
    /// <returns></returns>
    public static IServiceCollection AddEndpointParameterFluentValidation(this IServiceCollection collection, params Assembly[] scanAssemblies) {
        collection.AddTransient<IEndpointParameterValidator, EndpointParameterFluentValidator>();
        if (scanAssemblies?.Length > 0) {
            AssemblyScanner.FindValidatorsInAssemblies(scanAssemblies)
                           .ForEach(x => collection.AddTransient(x.InterfaceType, x.ValidatorType));
        }
        return collection;
    }
}
#endif