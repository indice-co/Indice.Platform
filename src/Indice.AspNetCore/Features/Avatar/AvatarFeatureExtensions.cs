#nullable enable
using Indice.AspNetCore.Features;
using Indice.Services;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Adds feature extensions to the <see cref="IMvcBuilder"/>.</summary>
public static class AvatarFeatureExtensions {

    /// <summary>Add the Avatar feature to MVC.</summary>
    /// <param name="mvcBuilder">An interface for configuring MVC services.</param>
    /// <param name="palette">The color palette to use.</param>
    public static IMvcBuilder AddAvatars(this IMvcBuilder mvcBuilder, params AvatarColor[] palette) =>
        AddAvatars(mvcBuilder, options => options.Palette = palette);

    /// <summary>Add the Avatar feature to MVC.</summary>
    /// <param name="mvcBuilder">An interface for configuring MVC services.</param>
    /// <param name="configureOptions">Action to configure the available options</param>
    public static IMvcBuilder AddAvatars(this IMvcBuilder mvcBuilder, Action<AvatarOptions> configureOptions) {
        mvcBuilder.ConfigureApplicationPartManager(apm => apm.FeatureProviders.Add(new AvatarFeatureProvider()));
        mvcBuilder.Services.AddResponseCaching();
        mvcBuilder.Services.AddAvatars(configureOptions);
        return mvcBuilder;
    }

    /// <summary>Add the Avatar feature to MVC.</summary>
    /// <param name="services">An interface for configuring MVC services.</param>
    /// <param name="configureOptions">Action to configure the available options</param>
    public static IServiceCollection AddAvatars(this IServiceCollection services, Action<AvatarOptions>? configureOptions = null) {
        var options = new AvatarOptions();
        configureOptions?.Invoke(options);
        services.AddSingleton(options);
        services.AddSingleton(sp => new AvatarGenerator(options));
        services.AddOutputCache();
        return services;
    }
}
#nullable disable