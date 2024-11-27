#nullable enable
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.AspNetCore.Features.Avatar;

/// <summary>Extension methods to register avatars endpoints.</summary>
public static class AvatarsApi
{
    /// <summary>Maps the sign in logs endpoints.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static IEndpointRouteBuilder MapAvatars(this IEndpointRouteBuilder routes) {
        var group = routes.MapGroup("avatar").ExcludeFromDescription();
        group.WithTags("Avatar");
        group.AllowAnonymous()
              .CacheOutput(policy=> policy.Expire(TimeSpan.FromMinutes(10))
                                          .SetAuthorized()
                                          .SetVaryByRouteValue("fullname", "size", "ext", "background"));
        group.MapGet("{fullname}", AvatarsHandlers.GetAvatar1);
        group.MapGet("{fullname}.{ext:regex(jpg|png|webp)}", AvatarsHandlers.GetAvatar1);
        group.MapGet("{fullname}/{size?}", AvatarsHandlers.GetAvatar2);
        group.MapGet("{fullname}/{size}.{ext:regex(jpg|png|webp)}", AvatarsHandlers.GetAvatar3);
        group.MapGet("{fullname}/{size}/{background}", AvatarsHandlers.GetAvatar);
        group.MapGet("{fullname}/{size}/{background}.{ext:regex(jpg|png|webp)}", AvatarsHandlers.GetAvatar);
        group.MapGet("", AvatarsHandlers.GetAvatarFull);

        return routes;
    }
}
#nullable disable