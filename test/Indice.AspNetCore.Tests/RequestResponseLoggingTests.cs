using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Indice.AspNetCore.Tests;

public class RequestResponseLoggingTests : IAsyncLifetime
{
    // Constants
    private const string BASE_URL = "https://server";
    // Private fields
    private readonly IHost _host;
    private HttpClient _httpClient = null!;
    private readonly ITestOutputHelper _output;

    public RequestResponseLoggingTests(ITestOutputHelper output) {
        _output = output;
        var builder = new HostBuilder();
        builder.ConfigureWebHost(webBuilder => {
            webBuilder.ConfigureAppConfiguration(builder => {
            });
            webBuilder.ConfigureServices((ctx, services) => {
                services.AddRouting();
            });
            webBuilder.Configure(app => {
                // This middleware is used to test the response body writing after all other middlewares are done.
                app.UseMiddleware<ResponseBodyWriterTestMiddlerware>();


                app.UseRequestResponseLogging();
                
                app.UseRouting();
                app.UseEndpoints(e => {
                    e.MapGet("/time", () => Results.NotFound(new { UtcNow = DateTime.UtcNow }));
                });
            });
            webBuilder.UseTestServer();
        });
        _host = builder.Build();
        
    }

    public async Task InitializeAsync() {
        await _host.StartAsync();
        var server = _host.GetTestServer();
        var handler = server.CreateHandler();
        _httpClient = new HttpClient(handler) {
            BaseAddress = new Uri(BASE_URL)
        };
    }

    public async Task DisposeAsync() {
        await((IAsyncDisposable)_host).DisposeAsync();
    }

    #region generic tests
    [Fact]
    public async Task Can_Get() {
        _ = await _httpClient.GetAsync(new Uri($"{BASE_URL}/tests/time"));
    }
    #endregion
}

class ResponseBodyWriterTestMiddlerware
{
    private readonly RequestDelegate _next;

    public ResponseBodyWriterTestMiddlerware(RequestDelegate next) {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task Invoke(HttpContext context, ILogger<string> logger) {
        await _next(context);

        // As a middleware uses WriteAsync to write to the response body, appending some data
        await context.Response.BodyWriter.WriteAsync(new ReadOnlyMemory<byte>([42, 42, 42, 42]));
    }
}
