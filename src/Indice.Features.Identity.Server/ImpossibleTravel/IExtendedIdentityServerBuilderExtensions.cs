using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Server;
using Indice.Features.Identity.Server.ImpossibleTravel;
using Indice.Features.Identity.Server.ImpossibleTravel.Services;

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
        builder.Services.AddScoped(typeof(ImpossibleTravelDetector<>).MakeGenericType(typeof(TUser)));
        return builder;
    }
}
