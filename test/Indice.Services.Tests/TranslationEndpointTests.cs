﻿using Microsoft.AspNetCore.Builder;
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
            services.AddRouting();

            services.AddTranslationGraph((o) => {
                o.DefaultTranslationsBaseName = "Resources.TranslationsApi";
                o.DefaultTranslationsLocation = typeof(EndpointTests).Assembly.GetName().Name;
                o.AddResource("https://yourdomainhere.com", "/http-translations.{lang:culture}.json", translationsLocation: "/api/translations.{0}.json"); // alternate assemby different resex. different path
            });
            services.AddDecorator<IStringLocalizerFactory, HttpStringLocalizerFactory>();
            services.AddHttpClient(nameof(HttpStringLocalizer));
            services.Configure<HttpStringLocalizerOptions>(options => {
                options.HttpLocations.Add("https://yourdomainhere.com/api/translations.{0}.json");
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
        _ = await getTranslationsAsync("/http-translations.el.json");
    }
    #endregion


    public Task InitializeAsync() {
        return Task.CompletedTask;
    }
    public async Task DisposeAsync() {
        await _serviceProvider.DisposeAsync();
    }
}