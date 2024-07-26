#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using Microsoft.AspNetCore.Routing;
using System.Text.Json.Nodes;
using Indice.AspNetCore.Views;
using Newtonsoft.Json.Linq;

namespace Indice.Services.Tests;
public class EndpointTests : IAsyncDisposable
{
    // Constants
    private const string BASE_URL = "https://server";
    // Private fields
    private readonly HttpClient _httpClient;
    private readonly ITestOutputHelper _output;
    private ServiceProvider _serviceProvider;

    public EndpointTests(ITestOutputHelper output) {
        _output = output;
        var builder = new WebHostBuilder();
        builder.ConfigureAppConfiguration(builder => {
            builder.AddInMemoryCollection(new Dictionary<string, string> {
                ["MySection:MyKey"] = "TestValue"
            });
        });
        builder.ConfigureServices(services => {
            var configuration = services.BuildServiceProvider().GetService<IConfiguration>();
            services.AddTransient<IEventDispatcherFactory, DefaultEventDispatcherFactory>();
            services.AddRouting();

            services.AddTranslationGraph((o) => {
                o.DefaultTranslationsBaseName = "Resources.TranslationsApi";
                o.DefaultTranslationsLocation = typeof(EndpointTests).Assembly.GetName().Name;
                o.AddResource("Resources.Alternate.TranslationsAlternateSource"); // same assemby different resex. same path
                o.AddResource("Resources.Alternate.TranslationsAlternateSource", "/translations-alternate.{lang:culture}.json"); // same assemby different resex. different path
                o.AddResource("Resources.TranslationsApi", "/translations-original.{lang:culture}.json"); // same assemby different resex. different path
                o.AddResource("TranslationsAlternateSource2", "/translations-alternate.{lang:culture}.json", translationsLocation: typeof(ViewsMarker).Assembly.GetName().Name); // alternate assemby different resex. different path
            });
            _serviceProvider = services.BuildServiceProvider();
        });
        builder.Configure(app => {
            app.UseRouting();
            app.UseEndpoints(e => e.MapTranslationGraph());
        });
        var server = new TestServer(builder);
        var handler = server.CreateHandler();
        _httpClient = new HttpClient(handler) {
            BaseAddress = new Uri(BASE_URL)
        };
    }

    #region Facts
    [Fact]
    public async Task Test_GetTranslatio_English() {
        var translationResponse = await _httpClient.GetAsync("/translations.en.json");
        var translationResponseJson = await translationResponse.Content.ReadAsStringAsync();
        if (!translationResponse.IsSuccessStatusCode) {
            _output.WriteLine(translationResponseJson);
        }

        Assert.True(translationResponse.IsSuccessStatusCode);
        Assert.NotEmpty(translationResponseJson);
    }
    [Fact]
    public async Task Test_GetTranslation_Greek() {
        var translationResponse = await _httpClient.GetAsync("/translations.el.json");
        var translationResponseJson = await translationResponse.Content.ReadAsStringAsync();
        if (!translationResponse.IsSuccessStatusCode) {
            _output.WriteLine(translationResponseJson);
        }

        Assert.True(translationResponse.IsSuccessStatusCode);
        Assert.NotEmpty(translationResponseJson);
    }
    [Fact]
    public async Task Test_GetTranslation_For_Culture_Not_Exists() {
        var translationResponse = await _httpClient.GetAsync("/translations.ru.json");
        var translationResponseJson = await translationResponse.Content.ReadAsStringAsync();
        if (!translationResponse.IsSuccessStatusCode) {
            _output.WriteLine(translationResponseJson);
        }

        Assert.True(translationResponse.IsSuccessStatusCode);
        Assert.NotEmpty(translationResponseJson);
    }
    [Fact]
    public async Task Test_GetTranslation_For_Invalid_Culture() {
        var translationResponse = await _httpClient.GetAsync("/translations.invalid.json");
        Assert.False(translationResponse.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Test_GetTranslation_From_Multiple_Sources() {
        var getTranslationsAsync = async (string routePattern) => {
            var response = await _httpClient.GetAsync(routePattern);
            var json = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) {
                _output.WriteLine(json);
            }

            Assert.True(response.IsSuccessStatusCode);
            return json;
        };
        var defaultJson = await getTranslationsAsync("/translations.el.json");
        var alternateJson = await getTranslationsAsync("/translations-alternate.el.json");
        var originalJson = await getTranslationsAsync("/translations-original.el.json");

        var translations = JsonNode.Parse(defaultJson);
        var value = translations["alternate"]["key"]["override"].GetValue<string>();
        Assert.Equal("overriden", value);

        translations = JsonNode.Parse(alternateJson);
        value = translations["alternate"]["key"]["override"].GetValue<string>();
        var value2 = translations["alternate"]["key"]["additional"].GetValue<string>();
        Assert.Equal("overriden", value);
        Assert.Equal("assembly", value2);

        translations = JsonNode.Parse(originalJson);
        value = translations["alternate"]["key"]["override"].GetValue<string>();
        Assert.Equal("original", value);
    }
    #endregion

    public async ValueTask DisposeAsync() {
        await _serviceProvider.DisposeAsync();
    }
}


#endif