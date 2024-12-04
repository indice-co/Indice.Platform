using System.Security.Claims;
using Bogus.DataSets;
using Indice.AspNetCore.Filters;
using Indice.Extensions;
using Indice.Features.Identity.Server;
using Indice.Features.Identity.Server.Manager;
using Indice.Features.Identity.Server.Manager.Models;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing;

/// <summary>Contains operations for managing a user's account.</summary>
public static class PictureApi
{
    internal const string CacheTagPrefix = "Picture";
    /// <summary>Adds Indice Identity Server user profile picture endpoints.</summary>
    /// <param name="routes">Indice Identity Server route builder.</param>
    public static IEndpointRouteBuilder MapProfilePictures(this IdentityServerEndpointRouteBuilder routes) {

        var options = routes.GetEndpointOptions();

        var group = routes.MapGroup($"{options.ApiPrefix}");
        group.WithTags("MyAccount");
        group.WithGroupName("identity");
        var allowedScopes = new[] { options.ApiScope }.Where(x => x is not null).Cast<string>().ToArray();
        group.RequireAuthorization(builder => builder
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme)
        );
        group.WithOpenApi();
        group.ProducesProblem(StatusCodes.Status500InternalServerError)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .InvalidateCacheTag(CacheTagPrefix, [], [BasicClaimTypes.Subject])
             .InvalidateCacheTag(CacheTagPrefix, ctx => [new("userId", ctx.User.FindSubjectId())])
             .InvalidateCacheTag(CacheTagPrefix, ctx => [new("userId", ctx.User.FindSubjectId()!.ToSha256Hex())]);

        group.MapPut("my/account/picture", PictureHandlers.SaveMyPicture)
             .WithName(nameof(PictureHandlers.SaveMyPicture))
             .WithSummary("Create or update profile picture of the current user.")
             .LimitUpload(options.Avatar.MaxFileSize, options.Avatar.AcceptableFileExtensions)
             .WithParameterValidation<ImageUploadRequest>()
             .Accepts<ImageUploadRequest>("multipart/form-data")
             .AddOpenApiSecurityRequirement("oauth2", allowedScopes)
             .RequireRateLimiting(IdentityEndpoints.RateLimiter.Policies.UploadPicture);

        group.MapDelete("my/account/picture", PictureHandlers.ClearMyPicture)
             .WithName(nameof(PictureHandlers.ClearMyPicture))
             .WithSummary("Clear profile picture from the current user.")
             .AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        var getMyPicture = routes.MapGroup($"{options.ApiPrefix}");
        getMyPicture.WithTags("MyAccount");
        getMyPicture.RequireAuthorization(builder => builder.RequireAuthenticatedUser())
                    .CacheOutput(policy => policy.SetVaryByRouteValue(["size", "format"])
                                              .SetVaryByQuery(["size"])
                                              .SetAutoTag()
                                              .SetAuthorized(ctx => ctx.User.FindSubjectId()!))
                    .WithCacheTag(CacheTagPrefix, [], [BasicClaimTypes.Subject]);

        getMyPicture.MapGet("my/account/picture", PictureHandlers.GetMyPicture)
            .WithName(nameof(PictureHandlers.GetMyPicture))
            .WithSummary("Get my profile picture.")
            .AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        getMyPicture.MapGet("my/account/picture/{size}", PictureHandlers.GetMyPictureSize)
            .WithName(nameof(PictureHandlers.GetMyPictureSize))
            .WithSummary("Get my profile picture.")
            .AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        getMyPicture.MapGet("my/account/picture.{format:regex(jpg|png|webp)}", PictureHandlers.GetMyPictureFormat)
            .WithName(nameof(PictureHandlers.GetMyPictureFormat))
            .WithSummary("Get my profile picture.")
            .AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        getMyPicture.MapGet("my/account/picture/{size}.{format:regex(jpg|png|webp)}", PictureHandlers.GetMyPictureSizeFormat)
            .WithName(nameof(PictureHandlers.GetMyPictureSizeFormat))
            .WithSummary("Get my profile picture.")
            .AddOpenApiSecurityRequirement("oauth2", allowedScopes);
        if (!options.Avatar.Enabled) { // disable only public access
            return routes;
        }
        var publicPictureGroup = routes.MapGroup("/");
        publicPictureGroup.WithTags(CacheTagPrefix);
        publicPictureGroup.WithGroupName("identity");
        publicPictureGroup.ExcludeFromDescription();
        publicPictureGroup.AllowAnonymous();
        publicPictureGroup.CacheOutput(policy => policy.SetVaryByRouteValue(["userId", "size", "format"])
                                                       .SetVaryByQuery(["size"])
                                                       .SetAutoTag()
                                                       .SetAuthorized()
                                                       //.Expire(TimeSpan.FromMinutes(5))
                                                       )
                          .WithCacheTag(CacheTagPrefix, ["userId"]);

        publicPictureGroup.MapGet("pictures/{userId}", PictureHandlers.GetAccountPicture)
             .WithName(nameof(PictureHandlers.GetAccountPicture))
             .WithSummary("Get user's profile picture.");

        publicPictureGroup.MapGet("pictures/{userId}/{size}", PictureHandlers.GetAccountPictureSize)
             .WithName(nameof(PictureHandlers.GetAccountPictureSize))
             .WithSummary("Get user's profile picture.");

        publicPictureGroup.MapGet("pictures/{userId}.{format:regex(jpg|png|webp)}", PictureHandlers.GetAccountPictureFormat)
             .WithName(nameof(PictureHandlers.GetAccountPictureFormat))
             .WithSummary("Get user's profile picture.");

        publicPictureGroup.MapGet("pictures/{userId}/{size}.{format:regex(jpg|png|webp)}", PictureHandlers.GetAccountPictureSizeFormat)
             .WithName(nameof(PictureHandlers.GetAccountPictureSizeFormat))
             .WithSummary("Get user's profile picture.");

        return group;
    }
}
