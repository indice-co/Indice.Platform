#if NET8_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using Indice.Services.Tests.Types;

namespace Indice.Services.Tests;

public class TranslationEndpointTests : IAsyncLifetime
{
    // Constants
    private const string BASE_URL = "https://server";
    // Private fields
    private readonly HttpClient _httpClient;
    private readonly ITestOutputHelper _output;
    private ServiceProvider _serviceProvider;


    public TranslationEndpointTests(ITestOutputHelper output) {
        _output = output;
        var builder = new WebHostBuilder();
        builder.ConfigureAppConfiguration(builder => {
            builder.AddInMemoryCollection(new Dictionary<string, string> {
                ["MySection:MyKey"] = "TestValue"
            });
        });
        builder.ConfigureServices(services => {
            var configuration = services.BuildServiceProvider().GetService<IConfiguration>();
            
            services.AddRouting();

            services.AddTranslationGraph((o) => {
                o.DefaultTranslationsBaseName = "Resources.TranslationsApi";
                o.DefaultTranslationsLocation = typeof(EndpointTests).Assembly.GetName().Name;
                o.AddResource(
                    translationsBaseName: "https://raw.githubusercontent.com", 
                    endpointRoutePattern: "/http-translations.{lang:culture}.json", 
                    translationsLocation: "/ngx-translate/example/refs/heads/master/src/assets/i18n/{0}.json"); // alternate assemby different resex. different path
            });
            services.AddDecorator<IStringLocalizerFactory, HttpStringLocalizerFactory>();
            services.AddHttpClient(nameof(HttpStringLocalizer));
            services.Configure<HttpStringLocalizerOptions>(options => {
                options.HttpLocations.Add("https://raw.githubusercontent.com/ngx-translate/example/refs/heads/master/src/assets/i18n/{0}.json");
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

    [Fact(Skip = "Work in progress")]
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
        var defaultJson = await getTranslationsAsync("/http-translations.en.json");
    }
    

    public Task InitializeAsync() {
        return Task.CompletedTask;
    }
    public async Task DisposeAsync() {
        await _serviceProvider.DisposeAsync();
    }
}

#endif