using Indice.AspNetCore.Views;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Indice.AspNetCore.Tests;

public class TestServerFixture : WebApplicationFactory<TestStartup>
{
    private TestServer? _server;

    public TService GetRequiredService<TService>() where TService : notnull {
        if (_server == null) {
            EnsureServer();
        }
        return _server!.Host.Services.GetRequiredService<TService>();
    }

    public new TestServer Server {
        get {
            EnsureServer();
            return _server!;
        }
    }

    private void EnsureServer() {
        if (_server == null) {
            var builder = CreateWebHostBuilder();

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

            ConfigureWebHost(builder);
            _server = new TestServer(builder);
        }
    }

    protected override IWebHostBuilder CreateWebHostBuilder() {
        var hostBuilder = new WebHostBuilder();
        hostBuilder.ConfigureAppConfiguration((context, builder) => {
            context.HostingEnvironment.ApplicationName = typeof(ViewsMarker).Assembly.GetName().Name!;
        });
        hostBuilder.UseStartup<TestStartup>();
        return hostBuilder;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        // Custom configuration can go here without calling the base class
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
        }
        // Don't call base.Dispose() to avoid disposing the base class server
    }
}
