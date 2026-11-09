using System.Reflection;
using Indice.AspNetCore.EmbeddedUI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>Extension methods on <see cref="IApplicationBuilder"/>, used to register the <see cref="SpaUIMiddleware{TOptions}"/> middleware.</summary>
/// <example>https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/write?view=aspnetcore-5.0#middleware-extension-method</example>
public static class SpaUIMiddlewareExtensions
{
    /// <summary>Registers the single page application, using the provided options.</summary>
    /// <typeparam name="TOptions">The type of options.</typeparam>
    /// <param name="builder">Defines a class that provides the mechanisms to configure an application's request pipeline.</param>
    /// <param name="embeddedUIRoot">Embedded UI root folder name.</param>
    /// <param name="assembly">The assembly containing the embedded resources.</param>
    /// <param name="optionsAction">Options for configuring <see cref="SpaUIMiddleware{TOptions}"/> middleware.</param>
    public static IApplicationBuilder UseSpaUI<TOptions>(this IApplicationBuilder builder, string embeddedUIRoot = "spa-ui-dist", Assembly? assembly = null, Action<TOptions>? optionsAction = null) where TOptions : SpaUIOptions, new() {
        assembly ??= Assembly.GetCallingAssembly();
        var configuration = builder.ApplicationServices.GetRequiredService<IConfiguration>();
        
        var options = new TOptions {
            Version = GetVersion(assembly),
            Authority = configuration["General:Authority"],
            Host = configuration["General:Host"]
        };

        optionsAction?.Invoke(options);
        if (options.Enabled) {
            builder.UseMiddleware<SpaUIMiddleware<TOptions>>(options, embeddedUIRoot, assembly);
        }
        return builder;
    }

    /// <summary>Registers the single page application, using the provided options.</summary>
    /// <param name="builder">Defines a class that provides the mechanisms to configure an application's request pipeline.</param>
    /// <param name="embeddedUIRoot">Embedded UI root folder name.</param>
    /// <param name="assembly">The assembly containing the embedded resources.</param>
    /// <param name="optionsAction">Options for configuring <see cref="SpaUIMiddleware{TOptions}"/> middleware.</param>
    public static IApplicationBuilder UseSpaUI(this IApplicationBuilder builder, string embeddedUIRoot = "spa-ui-dist", Assembly? assembly = null, Action<SpaUIOptions>? optionsAction = null) =>
        builder.UseSpaUI<SpaUIOptions>(embeddedUIRoot, assembly, optionsAction);

    private static string GetVersion(Assembly assembly) {
        var version = assembly.GetName().Version!.ToString(fieldCount: 3);
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Split('+')[0];
        if (!string.IsNullOrWhiteSpace(informationalVersion) && informationalVersion.StartsWith($"{version}-")) {
            version = informationalVersion;
        }
        return version;
    }
}
