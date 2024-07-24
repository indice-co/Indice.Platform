#if NET8_0_OR_GREATER
#nullable enable
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Extension methods to configure the Translations json endpoint. 
/// </summary>
public static class TranslationsGraphFeatureExtensions
{

    /// <summary>
    /// Adds translation dependencies. This will configure a resex file key value pair as source and produce a json.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureAction">The action to configure the translations endpoint source of key value pairs</param>
    /// <returns>The service collection for further configuration</returns>
    public static IServiceCollection AddTranslationGraph(this IServiceCollection services, Action<TranslationsGraphOptions>? configureAction = null) {
        services.AddLocalization();
        services.Configure<RouteOptions>(options => options.ConstraintMap.Add("culture", typeof(CultureRouteConstraint)));
        var options = new TranslationsGraphOptions();
        configureAction?.Invoke(options);
        services.Configure<TranslationsGraphOptions>((o) => {
            o.TranslationsBaseName = options.TranslationsBaseName;
            o.TranslationsLocation = options.TranslationsLocation;
            o.EndpointRoutePattern = options.EndpointRoutePattern;
        });
        return services;
    }

    /// <summary>
    /// Maps the Json Translations endpoint.
    /// </summary>
    /// <param name="routes">The endpoint route builder</param>
    /// <returns>The builder for further configureation</returns>
    
    public static IEndpointRouteBuilder MapTranslationGraph(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<TranslationsGraphOptions>>().Value;
        routes.MapGet(options.EndpointRoutePattern, (string lang, IStringLocalizerFactory factory) => {
            var strings = factory.Create(options.TranslationsBaseName, options.TranslationsLocation);
            return TypedResults.Ok(strings.ToObjectGraph(new System.Globalization.CultureInfo(lang)));
        });
        return routes;
    }
}

/// <summary>
/// Translation json options. Will be used to configure <see cref="TranslationsGraphFeatureExtensions"/>
/// </summary>
public class TranslationsGraphOptions 
{
    /// <summary>
    /// A dot dlimited path to the folder containing the Resex file with the translations key values. Defaults to <strong>"Resources.UiTranslations"</strong>
    /// </summary>
    public string TranslationsBaseName { get; set; } = "Resources.UiTranslations";

    /// <summary>
    /// The assembly name containing the translation resex files as embeded resources. Defaults to <strong>Assembly.GetEntryAssembly()!.GetName().Name!</strong>
    /// </summary>
    public string TranslationsLocation { get; set; } = Assembly.GetEntryAssembly()!.GetName().Name!;
    /// <summary>
    /// The endpoint route pattern defaults to <strong>"/translations.{lang:culture}.json"</strong>. If changes are made to the path we must paintain the lang parameter.
    /// </summary>
    [StringSyntax("Route")]
    public string EndpointRoutePattern { get; set; } = "/translations.{lang:culture}.json";
}
#nullable disable
#endif