using Indice.AspNetCore.Views;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Indice.AspNetCore.Tests;

public class TestServerFixture : WebApplicationFactory<TestStartup>
{
    private IHost? _host;
    private TestServer? _server;

    public TService GetRequiredService<TService>() where TService : notnull {
        if (_server == null) {
            EnsureServer();
        }
        return _host!.Services.GetRequiredService<TService>();
    }

    public new TestServer Server {
        get {
            EnsureServer();
            return _server!;
        }
    }

    private void EnsureServer() {
        if (_server == null) {
            _host = new HostBuilder().ConfigureWebHost(builder => {
                builder.UseTestServer();
                builder.ConfigureAppConfiguration((context, _) => {
                    context.HostingEnvironment.ApplicationName = typeof(ViewsMarker).Assembly.GetName().Name!;
                });
                builder.UseStartup<TestStartup>();

                // Find the solution root and set content root to the views project
                var solutionRoot = FindSolutionRoot();
                if (solutionRoot != null) {
                    var viewsPath = Path.Combine(solutionRoot, "test", "Indice.AspNetCore.Views");
                    if (Directory.Exists(viewsPath)) {
                        builder.UseContentRoot(viewsPath);
                    } else {
                        // Fallback to solution root if views path doesn't exist
                        builder.UseContentRoot(solutionRoot);
                    }
                }
            }).Start();
            _server = _host.GetTestServer();
        }
    }

    private static string? FindSolutionRoot() {
        var currentDirectory = Directory.GetCurrentDirectory();
        var directory = new DirectoryInfo(currentDirectory);

        while (directory != null) {
            // Look for .slnx files first, then fall back to .sln files
            if (directory.GetFiles("*.slnx").Length > 0 || directory.GetFiles("*.sln").Length > 0) {
                return directory.FullName;
            }
            directory = directory.Parent;
        }

        return null;
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _server?.Dispose();
            _host?.Dispose();
        }
        // Don't call base.Dispose() to avoid disposing the base class server
    }
}
