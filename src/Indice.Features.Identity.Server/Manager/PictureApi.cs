using Bogus.DataSets;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Server;
using Indice.Features.Identity.Server.Manager;
using Indice.Features.Identity.Server.Manager.Models;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing;

/// <summary>Contains operations for managing a user's account.</summary>
public static class PictureApi
{
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
             .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPut("my/account/picture", PictureHandlers.SaveMyPicture)
         .WithName(nameof(PictureHandlers.SaveMyPicture))
         .WithSummary("Create or update profile picture of the current user.")
         .LimitUpload(options.Avatar.MaxFileSize, options.Avatar.AcceptableFileExtensions)
         .WithParameterValidation<FileUploadRequest>()
         .Accepts<FileUploadRequest>("multipart/form-data")
         .AddOpenApiSecurityRequirement("oauth2", allowedScopes)
         .RequireRateLimiting(IdentityEndpoints.RateLimiter.Policies.UploadPicture);

        group.MapDelete("my/account/picture", PictureHandlers.ClearMyPicture)
             .WithName(nameof(PictureHandlers.ClearMyPicture))
             .WithSummary("Clear profile picture from the current user.")
             .AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        var getMyPicture = routes.MapGroup($"{options.ApiPrefix}");
        getMyPicture.WithTags("MyAccount");
        getMyPicture.RequireAuthorization(builder => builder
            .RequireAuthenticatedUser()
        );

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
        publicPictureGroup.WithTags("Picture");
        publicPictureGroup.WithGroupName("identity");
        publicPictureGroup.ExcludeFromDescription();
        publicPictureGroup.AllowAnonymous();
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


#if NET7_0_OR_GREATER
        publicPictureGroup.CacheOutput(policy => {
            policy.AddPolicy<DefaultTagCachePolicy>();
            policy.SetVaryByRouteValue(["userId", "size", "format"]);
            policy.SetVaryByQuery(["size"]);
            policy.SetCacheKeyPrefix((ctx) => {
                var key = $"Picture-userId:{ctx.GetRouteValue("userId")}";
                return key;
            });
        });
        //publicPictureGroup
        //    .CacheOutput(policy => policy.SetTagPrefix().SetAuthorized())
        //    .CacheAuthorized()
        //    .WithCacheTag("users", ["userId"]);
#endif

        return group;
    }
}
