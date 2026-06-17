using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Indice.AspNetCore.Features.Translations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

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
            o.ExcludeFromDescription = options.ExcludeFromDescription;
            o.ConfigureCachePolicy = options.ConfigureCachePolicy;
            o.AvailableLanguagesRoutePattern = options.AvailableLanguagesRoutePattern;
        });
        return services;
    }

    /// <summary>
    /// Adds translation resources. Used when we want to add translation endpoints with the
    /// default configuration without the need to configure options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="resourceName">A dot-delimited path to the folder containing the Resx file with the translation key-values. For example <strong>Resources.UiTranslations</strong>.</param>
    /// <param name="endpointRoutePattern">The route pattern for the translation endpoint.</param>
    /// <param name="resourceLocation">The assembly name containing the translation Resx files as embedded resources.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTranslationsResource(this IServiceCollection services, string resourceName, string? endpointRoutePattern, string? resourceLocation) {
        if(string.IsNullOrWhiteSpace(resourceName)) {
            throw new ArgumentException("Resource name must be provided", nameof(resourceName));
        }
        services.Configure<TranslationsGraphOptions>(options => {
            options.AddResource(resourceName, endpointRoutePattern, resourceLocation);
        });
        return services;
    }

    /// <summary>
    /// Maps the Json Translations endpoint.
    /// </summary>
    /// <param name="routes">The endpoint route builder</param>
    /// <returns>The builder for further configuration</returns>
    public static IEndpointRouteBuilder MapTranslationGraph(this IEndpointRouteBuilder routes) {
        routes.MapGraphs().MapAvailableLanguages();
        return routes;
    }

    /// <summary>
    /// Maps the Json Translations endpoint.
    /// </summary>
    /// <param name="routes">The endpoint route builder</param>
    /// <returns>The builder for further configuration</returns>
    private static IEndpointRouteBuilder MapGraphs(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<TranslationsGraphOptions>>().Value;
        var endpoints = options.GetEndpoints();
        int counter = 0;
        foreach (var endpoint in endpoints) {
            var operationName = "GetTranslations";
            if (counter > 0) {
                operationName += counter;
            }
            var translationRouteHandler = routes.MapGet(endpoint.Key, (string lang, IStringLocalizerFactory factory) => {
                var culture = new System.Globalization.CultureInfo(lang);
                var strings = endpoint.SelectMany(x => factory.Create(x.TranslationsBaseName, x.TranslationsLocation).GetAllStrings(culture, includeParentCultures: true));
                return TypedResults.Ok(strings.ToObjectGraph());
            })
            .WithDescription($"Get translations aggregate for {endpoint.First().TranslationsBaseName}")
            .WithName(operationName).WithTags("Translations");
            counter++;
            if (options.ExcludeFromDescription) {
                translationRouteHandler.ExcludeFromDescription();
            }
            if (options.ConfigureCachePolicy != null) {
                translationRouteHandler.CacheOutput(options.ConfigureCachePolicy);
            }
        }
        return routes;
    }

    /// <summary>
    /// Maps the available languages for translation.
    /// </summary>
    /// <param name="routes">The endpoint route builder</param>
    /// <returns>The builder for further configuration</returns>
    private static IEndpointRouteBuilder MapAvailableLanguages(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<TranslationsGraphOptions>>().Value;
        var localizationOptions = routes.ServiceProvider.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
        var availableLanguagesRouteHandler = routes.MapGet(options.AvailableLanguagesRoutePattern, () => {
            var availableLanguages = localizationOptions?.SupportedCultures?.Select(x => new UiLocale() {
                Lang = x.Name,
                NativeName = x.NativeName,
                EnglishName = x.EnglishName,
            }).ToList();
            return TypedResults.Ok(availableLanguages);
        })
        .WithDescription("Get available languages for translations")
        .WithName("GetAvailableLanguages").WithTags("Translations");
        if (options.ExcludeFromDescription) {
            availableLanguagesRouteHandler.ExcludeFromDescription();
        }
        if (options.ConfigureCachePolicy != null) {
            availableLanguagesRouteHandler.CacheOutput(options.ConfigureCachePolicy);
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
    /// The route for the available languages endpoint
    /// </summary>
    [StringSyntax("Route")]
    public string AvailableLanguagesRoutePattern { get; set; } = "/languages";
    /// <summary>
    /// Decides whether to enable swagger/openapi documentation for the endpoint
    /// </summary>
    public bool ExcludeFromDescription { get; set; } = true;

    /// <summary>
    /// Optional cache policy for the endpoint
    /// </summary>
    public Action<OutputCachePolicyBuilder>? ConfigureCachePolicy { get; set; }
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