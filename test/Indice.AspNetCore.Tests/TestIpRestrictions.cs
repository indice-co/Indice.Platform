using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Indice.AspNetCore.Tests;

public class TestIpRestrictions
{
    [Fact]
    public async Task MiddlewareTest_ReturnsNotFoundForIngoredPaths() {
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder => {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services => {
                        services.AddClientIpRestrinctions(options => {
                            options.HttpStatusCode = HttpStatusCode.Forbidden;
                            options.AddIpAddressList("MyWhiteList", "192.168.1.5");
                            options.MapPath("/admin", "192.168.1.5");
                            options.MapPath("/login", "MyWhiteList");
                            options.IgnorePath("/login?ReturnUrl=", "POST");
                            options.IgnorePath("/login?ReturnUrl=", "GET");
                        });
                    })
                    .Configure(app => {
                        app.UseMiddleware<FakeRemoteIpAddressMiddleware>();
                        app.UseClientIpRestrictions();
                    });
            })
            .StartAsync(cancellationToken: TestContext.Current.CancellationToken);

        var client = host.GetTestClient();
        var response = await client.GetAsync("/", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        response = await client.GetAsync("/login", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        response = await client.GetAsync("/admin", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        response = await client.GetAsync("/admin/home", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var server = host.GetTestServer();

        server.BaseAddress = new Uri("https://example.com");
        var context = await server.SendAsync(c => {
            c.Request.Method = HttpMethods.Post;
            c.Request.Path = "/login";
            c.Request.QueryString = new QueryString("?returnUrl=/docs");
        }, TestContext.Current.CancellationToken);

        Assert.Equal(404, context.Response.StatusCode);
    }
}
