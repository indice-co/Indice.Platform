using Indice.AspNetCore.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// A builder for web applications and services that has all the indice defaults preconfigured. This is a decorator for the inner <seealso cref="WebApplicationBuilder"/>
/// </summary>
public class IndiceWebApplicationBuilder : IHostApplicationBuilder
{
    private WebApplicationBuilder InnerBuilder { get; }

    /// <summary>
    /// constructs the <see cref="IndiceWebApplicationBuilder "/> given the inner builder.
    /// </summary>
    /// <param name="innerBuilder"></param>
    internal IndiceWebApplicationBuilder(WebApplicationBuilder innerBuilder) {
        InnerBuilder = innerBuilder;
    }

    /// <inheritdoc/>
    public IConfigurationManager Configuration => InnerBuilder.Configuration;

    /// <inheritdoc/>
    public IHostEnvironment Environment => InnerBuilder.Environment;

    /// <inheritdoc/>
    public ILoggingBuilder Logging => InnerBuilder.Logging;

    /// <inheritdoc/>
    public IMetricsBuilder Metrics => InnerBuilder.Metrics;

    /// <inheritdoc/>
    IDictionary<object, object> IHostApplicationBuilder.Properties => ((IHostApplicationBuilder)InnerBuilder).Properties;

    /// <inheritdoc/>
    public IServiceCollection Services => InnerBuilder.Services;
    
    /// <inheritdoc/>
    public ConfigureWebHostBuilder WebHost => InnerBuilder.WebHost;

    /// <inheritdoc/>
    void IHostApplicationBuilder.ConfigureContainer<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory, Action<TContainerBuilder>? configure) =>
        ((IHostApplicationBuilder)InnerBuilder).ConfigureContainer(factory, configure);

    /// <summary>
    /// Builds the <see cref="WebApplication"/>.
    /// </summary>
    /// <returns>A configured <see cref="WebApplication"/>.</returns>
    public WebApplication BuildDefault() => InnerBuilder.Build();

    /// <summary>
    /// Builds the <see cref="WebApplication"/> to the idice specifications.
    /// </summary>
    /// <returns>A configured <see cref="WebApplication"/>.</returns>
    public WebApplication Build() {
        var app = InnerBuilder.Build();

        LogStartupBanner();

        if (app.Configuration.UseCertificateForwarding()) {
            app.UseCertificateForwarding();
        }
        if (app.Configuration.ProxyEnabled()) {
            app.UseForwardedHeaders();
            app.UseHttpMethodOverride();
        }
        if (app.Configuration.UseRedirectToHost()) {
            var rewrite = new RewriteOptions();
            rewrite.Rules.Add(new RedirectToHostRewriteRule(app.Configuration.GetHost()));
            app.UseRewriter(rewrite);
        }
        if (app.Configuration.UseHttpsRedirection()) {
            app.UseHttpsRedirection();
        }
        if (app.Configuration.HstsEnabled()) {
            app.UseHsts();
        }

        app.UseCors();
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseAuthentication();
        app.UseAuthorization();
        if (app.Environment.IsDevelopment()) {
            app.UseDeveloperExceptionPage();
        }
        app.UseRequestLocalization();
        return app;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebApplication"/> class with preconfigured defaults.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <returns>The <see cref="WebApplication"/>.</returns>
    public static IndiceWebApplicationBuilder CreateBuilder(string[] args) =>
        new(WebApplication.CreateBuilder(args).AddMinimalApiDefaults());

    private readonly string[] Catchphrases = [
        "Platform Tools, Built to Scale",
        "Indice Platform: Libraries in Motion",
        "Platform Team: Build Once, Ship Everywhere",
        "Indice Platform — Tools That Power Teams"
        ];

    private void LogStartupBanner() {
        if (!Configuration.GetValue<bool?>("General:StartupBannerEnabled") ?? Environment.IsProduction()) {
            return;
        }

        const int bannerInnerWidth = 46;
        var catchphrase = Catchphrases[Random.Shared.Next(Catchphrases.Length)];
        var centeredCatchphrase = CenterText(catchphrase, bannerInnerWidth);

        var banner = $"""

                              ╔══════════════════════════════════════════════╗
                              ║                                              ║
                              ║   ██╗███╗   ██╗██████╗ ██╗ ██████╗███████╗   ║
                              ║   ██║████╗  ██║██╔══██╗██║██╔════╝██╔════╝   ║
                              ║   ██║██╔██╗ ██║██║  ██║██║██║     █████╗     ║
                              ║   ██║██║╚██╗██║██║  ██║██║██║     ██╔══╝     ║
                              ║   ██║██║ ╚████║██████╔╝██║╚██████╗███████║   ║
                              ║   ╚═╝╚═╝  ╚═══╝╚═════╝ ╚═╝ ╚═════╝╚══════╝   ║
                              ║                                              ║
                              ║{centeredCatchphrase}║
                              ║                                              ║
                              ╚══════════════════════════════════════════════╝

                              """;
        if (Console.IsOutputRedirected) {
            Console.WriteLine(banner);
            return;
        }

        var originalColor = Console.ForegroundColor;
        try {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(banner);
        } finally {
            Console.ForegroundColor = originalColor;
        }

        static string CenterText(string text, int width) {
            if (string.IsNullOrWhiteSpace(text)) {
                return new string(' ', width);
            }

            if (text.Length >= width) {
                return text[..width];
            }

            var padding = width - text.Length;
            var left = padding / 2;
            var right = padding - left;
            return string.Concat(new string(' ', left), text, new string(' ', right));
        }
    }
}
