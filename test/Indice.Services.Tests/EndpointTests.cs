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
                o.TranslationsBaseName = "Resources.TranslationsApi";
                o.TranslationsLocation = typeof(EndpointTests).Assembly.GetName().Name;
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
        //Create the Campaign
        var translationResponse = await _httpClient.GetAsync("/translations.en.json");
        var translationResponseJson = await translationResponse.Content.ReadAsStringAsync();
        if (!translationResponse.IsSuccessStatusCode) {
            _output.WriteLine(translationResponseJson);
        }

        Assert.True(translationResponse.IsSuccessStatusCode);
        Assert.NotEmpty(translationResponseJson);
    }
    [Fact]
    public async Task Test_GetTranslatio_Greek() {
        //Create the Campaign
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
        //Create the Campaign
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
        //Create the Campaign
        var translationResponse = await _httpClient.GetAsync("/translations.invalid.json");
        Assert.False(translationResponse.IsSuccessStatusCode);
    }
    #endregion

    public async ValueTask DisposeAsync() {
        await _serviceProvider.DisposeAsync();
    }
}


#endif