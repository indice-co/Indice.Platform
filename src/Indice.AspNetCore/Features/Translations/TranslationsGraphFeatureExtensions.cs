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
using System.Text.RegularExpressions;

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
            o.DefaultTranslationsBaseName = options.DefaultTranslationsBaseName;
            o.DefaultTranslationsLocation = options.DefaultTranslationsLocation;
            o.DefaultEndpointRoutePattern = options.DefaultEndpointRoutePattern;
            o.Resources.AddRange(options.Resources);
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
        var endpoints = options.GetEndpoints();
        int counter = 0;
        foreach (var endpoint in endpoints) {
            var operationName = "GetTranslations";
            if (counter > 0) {
                operationName += counter;
            } 
            routes.MapGet(endpoint.Key, (string lang, IStringLocalizerFactory factory) => {
                var culture = new System.Globalization.CultureInfo(lang);
                var strings = endpoint.SelectMany(x => factory.Create(x.TranslationsBaseName, x.TranslationsLocation).GetAllStrings(culture, includeParentCultures: true));
                return TypedResults.Ok(strings.ToObjectGraph());
            })
            .WithDescription($"Get translations aggregate for {endpoint.First().TranslationsBaseName}")
            .WithName(operationName);
            counter++;
        }
        return routes;
    }
}

/// <summary>
/// Translation json options. Will be used to configure <see cref="TranslationsGraphFeatureExtensions"/>
/// </summary>
public class TranslationsGraphOptions 
{
    /// <summary>
    /// Additional endpoints/resources
    /// </summary>
    internal List<TranslationGraphResource> Resources { get; } = [];

    /// <summary>
    /// A dot dlimited path to the folder containing the Resex file with the translations key values. Defaults to <strong>"Resources.UiTranslations"</strong>
    /// </summary>
    public string DefaultTranslationsBaseName { get; set; } = "UiTranslations";

    /// <summary>
    /// The assembly name containing the translation resex files as embeded resources. Defaults to <strong>Assembly.GetEntryAssembly()!.GetName().Name!</strong>
    /// </summary>
    public string DefaultTranslationsLocation { get; set; } = Assembly.GetEntryAssembly()!.GetName().Name!;
    /// <summary>
    /// The endpoint route pattern defaults to <strong>"/translations.{lang:culture}.json"</strong>. If changes are made to the path we must paintain the lang parameter.
    /// </summary>
    [StringSyntax("Route")]
    public string DefaultEndpointRoutePattern { get; set; } = "/translations.{lang:culture}.json";

    /// <summary>
    /// Encapsulates the settings needed to run an enpoint
    /// </summary>
    public record TranslationGraphResource([StringSyntax("Route")] string EndpointRoutePattern, string TranslationsBaseName, string TranslationsLocation);

    /// <summary>
    /// adds additional endpoints/resources. Appart form the default settings
    /// </summary>
    /// <param name="translationsBaseName">A dot dlimited path to the folder containing the Resex file with the translations key values. For example <strong>Resources.UiTranslations</strong></param>
    /// <param name="endpointRoutePattern">The endpoint route pattern. If changes are made to the path we must paintain the <strong>{lang}</strong> parameter. Defaults to <strong>"/translations.{lang:culture}.json"</strong></param>
    /// <param name="translationsLocation">The assembly name containing the translation resex files as embeded resources.  Defaults to <strong>Assembly.GetEntryAssembly()!.GetName().Name!</strong></param>
    /// <returns></returns>
    public TranslationsGraphOptions AddResource(string translationsBaseName, [StringSyntax("Route")] string? endpointRoutePattern = null, string? translationsLocation = null) {
        var resource = new TranslationGraphResource(endpointRoutePattern ?? DefaultEndpointRoutePattern, translationsBaseName, translationsLocation ?? DefaultTranslationsLocation);
        Resources.Add(resource);
        return this;
    }

    /// <summary>
    /// Gets all available endpoint configurations groupd by endpoint route pattern in order to configure aspnet core endpoint routing.
    /// </summary>
    /// <returns></returns>
    public ILookup<string, TranslationGraphResource> GetEndpoints() {
        List<TranslationGraphResource> all = [new (DefaultEndpointRoutePattern, DefaultTranslationsBaseName, DefaultTranslationsLocation), ..Resources];
        return all.ToLookup(x => x.EndpointRoutePattern);
    }
}


#nullable disable
#endif