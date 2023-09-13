using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Server;
using Indice.Features.Identity.Server.ImpossibleTravel;
using Indice.Features.Identity.Server.ImpossibleTravel.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods on <see cref="IExtendedIdentityServerBuilder"/>.</summary>
public static class IExtendedIdentityServerBuilderExtensions
{
    /// <summary>Adds the required services for a mechanism that impossible travel login attempts.</summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <param name="builder">IdentityServer builder interface.</param>
    /// <param name="configure">Configure action for the impossible travel detector feature.</param>
    public static TBuilder AddImpossibleTravelDetector<TBuilder>(this TBuilder builder, Action<ImpossibleTravelDetectorOptions>? configure = null) where TBuilder : IIdentityServerBuilder =>
        builder.AddImpossibleTravelDetector<TBuilder, User>(configure);

    /// <summary>Adds the required services for a mechanism that impossible travel login attempts.</summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <typeparam name="TUser"></typeparam>
    /// <param name="builder">IdentityServer builder interface.</param>
    /// <param name="configure">Configure action for the impossible travel detector feature.</param>
    public static TBuilder AddImpossibleTravelDetector<TBuilder, TUser>(this TBuilder builder, Action<ImpossibleTravelDetectorOptions>? configure = null)
        where TBuilder : IIdentityServerBuilder
        where TUser : User {
        var impossibleTravelDetectorOptions = new ImpossibleTravelDetectorOptions();
        configure?.Invoke(impossibleTravelDetectorOptions);
        builder.Services.Configure<ImpossibleTravelDetectorOptions>(options => {
            options.AcceptableSpeed = impossibleTravelDetectorOptions.AcceptableSpeed;
        });
        builder.Services.AddScoped<ImpossibleTravelDetector<TUser>>();
        var serviceDescriptor = builder.Services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(ISignInGuard<TUser>));
        if (serviceDescriptor is not null) {
            builder.Services.Remove(serviceDescriptor);
        }
        builder.Services.TryAddScoped<ISignInGuard<TUser>, SignInGuard<TUser>>();
        return builder;
    }
}
