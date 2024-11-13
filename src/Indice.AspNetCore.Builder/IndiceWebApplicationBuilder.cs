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

        if (app.Configuration.ProxyEnabled()) {
            app.UseForwardedHeaders();
            app.UseHttpMethodOverride();
        }

        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseAuthentication();
        app.UseAuthorization();
        if (app.Environment.IsDevelopment()) {
            app.UseDeveloperExceptionPage();
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
        app.UseRequestLocalization();
        app.UseCors();
        return app;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebApplication"/> class with preconfigured defaults.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <returns>The <see cref="WebApplication"/>.</returns>
    public static IndiceWebApplicationBuilder CreateBuilder(string[] args) =>
        new(WebApplication.CreateBuilder(args).AddMinimalApiDefaults());
}
