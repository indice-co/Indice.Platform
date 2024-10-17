using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Indice.Services.Tests.Types;
public class HttpStringLocalizer : IStringLocalizer
{
    public HttpStringLocalizer(HttpClient httpClient, string baseName, string location) {
        HttpClient = httpClient;
        BaseName = baseName;
        Location = location;
    }

    public LocalizedString this[string name] => throw new NotImplementedException();

    public LocalizedString this[string name, params object[] arguments] => throw new NotImplementedException();

    public HttpClient HttpClient { get; }
    /// <summary>
    /// This is the host: https://www.indice.gr
    /// </summary>
    public string BaseName { get; }
    /// <summary>
    /// This is the path /api/translations.json?culture={0} or /api/translations.{0}.json as a masked string with a culture parameter.
    /// </summary>
    public string Location { get; }


    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) {
        var uriMask = BaseName.TrimEnd('/') + "/" + Location.TrimStart('/');
        var uri = new Uri(string.Format(uriMask, CultureInfo.CurrentCulture.TwoLetterISOLanguageName));
        var response = HttpClient.GetStreamAsync(uri).GetAwaiter().GetResult();
        var builder = new ConfigurationBuilder().AddJsonStream(response);
        var config = builder.Build();
        return config.AsEnumerable().Select(x => new LocalizedString(x.Key, x.Value ?? string.Empty));
    }
}

public class HttpStringLocalizerOptions 
{
    public HashSet<string> HttpLocations { get; set; } = [];

    /// <summary>
    /// This must have a mask for the current culture!!!
    /// </summary>
    /// <returns></returns>
    public HttpStringLocalizerOptions AddHttpEndpoint(string host,
#if NET7_0_OR_GREATER
        [StringSyntax("Route")]string path
#else
        string path
#endif
        ) {
        HttpLocations.Add(host + path);
        return this;
    }
}

public class HttpStringLocalizerFactory : IStringLocalizerFactory
{
    public HttpStringLocalizerFactory(IStringLocalizerFactory inner, IOptions<HttpStringLocalizerOptions> options, IHttpClientFactory httpClientFactory) {
        Inner = inner;
        Options = options;
        HttpClientFactory = httpClientFactory;
    }

    public IStringLocalizerFactory Inner { get; }
    public IOptions<HttpStringLocalizerOptions> Options { get; }
    public IHttpClientFactory HttpClientFactory { get; }

    public IStringLocalizer Create(Type resourceSource) => 
        CanHandle(resourceSource.Namespace, resourceSource.Name) ? 
        Create(resourceSource.Namespace, resourceSource.Name) : 
        Inner.Create(resourceSource);

    public IStringLocalizer Create(string baseName, string location) {
        if (!CanHandle(baseName, location)) {
            return Inner.Create(baseName, location);
        }
        var httpClient = HttpClientFactory.CreateClient(nameof(HttpStringLocalizer));
        return new HttpStringLocalizer(httpClient, baseName, location);
    }

    private bool CanHandle(string baseName, string location) => Options.Value.HttpLocations.Contains(baseName + location);
}

public static class HttpStringLocalizerFeatureExtensions
{
    public static IServiceCollection AddLocalizationHttpClient(this IServiceCollection services, Action<HttpStringLocalizerOptions> configureAction) {
        services.AddLocalization();
        services.AddDecorator<IStringLocalizerFactory, HttpStringLocalizerFactory>();
        services.AddHttpClient(nameof(HttpStringLocalizer));
        services.Configure(configureAction);
        return services;
    }
    public static IServiceCollection AddLocalizationHttpClient(this IServiceCollection services, IConfiguration configuration) {
        services.AddLocalization();
        services.AddDecorator<IStringLocalizerFactory, HttpStringLocalizerFactory>();
        services.AddHttpClient(nameof(HttpStringLocalizer));
        services.Configure<HttpStringLocalizerOptions>(configuration);
        return services;
    }
}