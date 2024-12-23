﻿using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using IdentityModel;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Server.Manager.Models;
using Indice.Features.Identity.Server.Options;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Indice.Extensions;

namespace Indice.Features.Identity.Server.Manager;

internal static partial class PictureHandlers
{
    internal static Task<Results<NoContent, NotFound, ValidationProblem>> SaveMyPicture(
        ImageUploadRequest request,
        ExtendedUserManager<User> userManager,
        LinkGenerator linkGenerator,
        IOptions<ExtendedEndpointOptions> endpointOptions,
        IOutputCacheStore cache,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        CancellationToken cancellationToken) => SaveUserPicture(request, userManager, linkGenerator, endpointOptions, cache, currentUser.FindSubjectId()!, httpContext, cancellationToken);

    internal static Task<Results<NoContent, NotFound, ValidationProblem>> ClearMyPicture(
        ExtendedUserManager<User> userManager,
        ExtendedIdentityDbContext<User, Role> dbContext,
        IOutputCacheStore cache,
        ClaimsPrincipal currentUser,
        CancellationToken cancellationToken) => ClearUserPicture(userManager, dbContext, cache, currentUser.FindSubjectId()!, cancellationToken);

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> SaveUserPicture(
        ImageUploadRequest request,
        ExtendedUserManager<User> userManager,
        LinkGenerator linkGenerator,
        IOptions<ExtendedEndpointOptions> endpointOptions,
        IOutputCacheStore cache,
        string userId,
        HttpContext httpContext,
        CancellationToken cancellationToken) {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null) {
            return TypedResults.NotFound();
        }
        if (!(request.File?.Length > 0)) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(request.File), "The file is mandatory"));
        }

        using var stream = request.File!.OpenReadStream();
        var maxSideSize = endpointOptions.Value.Avatar.AllowedSizes.Max();
        var result = await userManager.SetUserPictureAsync(user, stream, maxSideSize, request.Scale ?? 1, request.TranslateX ?? 0 , request.TranslateY ?? 0, viewPortSize: request.ViewPort ?? maxSideSize);
        if (!result.Succeeded) {
            return TypedResults.ValidationProblem(result.Errors.ToDictionary());
        }
        var route = linkGenerator.GetUriByName(httpContext, nameof(GetAccountPicture), new { userId = user.Id.ToSha256Hex() });
        if (route is not null) { 
            result = await userManager.ReplaceClaimAsync(user, JwtClaimTypes.Picture, route!);
        }
        if (!result.Succeeded) {
            return TypedResults.ValidationProblem(result.Errors.ToDictionary());
        }
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> ClearUserPicture(
        ExtendedUserManager<User> userManager,
        ExtendedIdentityDbContext<User, Role> dbContext,
        IOutputCacheStore cache,
        string userId,
        CancellationToken cancellationToken) {

        var user = await userManager.FindByIdAsync(userId);
        if (user == null) {
            return TypedResults.NotFound();
        }
        var result = await userManager.ClearUserPictureAsync(user);
        if (!result.Succeeded) {
            return TypedResults.ValidationProblem(result.Errors.ToDictionary());
        }
        return TypedResults.NoContent();
    }

    internal static Task<Results<FileStreamHttpResult, NotFound, ValidationProblem, RedirectHttpResult>> GetMyPicture(
        ExtendedUserManager<User> userManager,
        IOptions<ExtendedEndpointOptions> endpointOptions,
        ClaimsPrincipal currentUser,
        int? size,
        [FromQuery(Name = "d")] string? fallbackUrl) => GetUserPictureInternal(userManager, endpointOptions, currentUser.FindSubjectId()!, extension: null, size, fallbackUrl);

    internal static Task<Results<FileStreamHttpResult, NotFound, ValidationProblem, RedirectHttpResult>> GetMyPictureSize(
        ExtendedUserManager<User> userManager,
        IOptions<ExtendedEndpointOptions> endpointOptions,
        ClaimsPrincipal currentUser,
        int size,
        [FromQuery(Name = "d")] string? fallbackUrl) => GetUserPictureInternal(userManager, endpointOptions, currentUser.FindSubjectId()!, extension: null, size, fallbackUrl);

    internal static Task<Results<FileStreamHttpResult, NotFound, ValidationProblem, RedirectHttpResult>> GetMyPictureFormat(
        ExtendedUserManager<User> userManager,
        IOptions<ExtendedEndpointOptions> endpointOptions,
        ClaimsPrincipal currentUser,
        string format,
        int? size,
        [FromQuery(Name = "d")] string? fallbackUrl) => GetUserPictureInternal(userManager, endpointOptions, currentUser.FindSubjectId()!, extension: '.' + format, size, fallbackUrl);

    internal static Task<Results<FileStreamHttpResult, NotFound, ValidationProblem, RedirectHttpResult>> GetMyPictureSizeFormat(
        ExtendedUserManager<User> userManager,
        IOptions<ExtendedEndpointOptions> endpointOptions,
        ClaimsPrincipal currentUser,
        int size,
        string format,
        [FromQuery(Name = "d")] string? fallbackUrl) => GetUserPictureInternal(userManager, endpointOptions, currentUser.FindSubjectId()!, extension: '.' + format, size, fallbackUrl);

    internal static Task<Results<FileStreamHttpResult, NotFound, ValidationProblem, RedirectHttpResult>> GetAccountPicture(
        ExtendedUserManager<User> userManager,
        IOptions<ExtendedEndpointOptions> endpointOptions,
        string userId,
        int? size,
        [FromQuery(Name = "d")] string? fallbackUrl) => GetUserPictureInternal(userManager, endpointOptions, userId, extension: null, size, fallbackUrl);

    internal static Task<Results<FileStreamHttpResult, NotFound, ValidationProblem, RedirectHttpResult>> GetAccountPictureSize(
        ExtendedUserManager<User> userManager,
        IOptions<ExtendedEndpointOptions> endpointOptions,
        string userId,
        int size,
        [FromQuery(Name = "d")] string? fallbackUrl) => GetUserPictureInternal(userManager, endpointOptions, userId, extension: null, size, fallbackUrl);

    internal static Task<Results<FileStreamHttpResult, NotFound, ValidationProblem, RedirectHttpResult>> GetAccountPictureFormat(
        ExtendedUserManager<User> userManager,
        IOptions<ExtendedEndpointOptions> endpointOptions,
        string userId,
        string format,
        int? size,
        [FromQuery(Name = "d")] string? fallbackUrl) => GetUserPictureInternal(userManager, endpointOptions, userId, extension: '.' + format, size, fallbackUrl);

    internal static Task<Results<FileStreamHttpResult, NotFound, ValidationProblem, RedirectHttpResult>> GetAccountPictureSizeFormat(
        ExtendedUserManager<User> userManager,
        IOptions<ExtendedEndpointOptions> endpointOptions,
        string userId,
        int size,
        string format,
        [FromQuery(Name = "d")] string? fallbackUrl) => GetUserPictureInternal(userManager, endpointOptions, userId, extension: '.' + format, size, fallbackUrl);

    private static async Task<Results<FileStreamHttpResult, NotFound, ValidationProblem, RedirectHttpResult>> GetUserPictureInternal(
        ExtendedUserManager<User> userManager,
        IOptions<ExtendedEndpointOptions> endpointOptions,
        string pictureKey,
        string? extension,
        int? size,
        string? fallbackUrl) {
        if (size > 0 && !endpointOptions.Value.Avatar.AllowedSizes.Contains(size.Value)) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError("size", $"The specified size is not in the allowed list ({string.Join(',', endpointOptions.Value.Avatar.AllowedSizes)})"));
        }
        if (size == endpointOptions.Value.Avatar.AllowedSizes.Max()) {
            size = null;
        }
        (var stream, var contentType, var userExists) = await userManager.FindPictureByKeyAsync(pictureKey, GetImageContentType(extension), size);
        if (!userExists) {
            return TypedResults.NotFound();
        }
        if (stream is null) {
            if (fallbackUrl is not null && fallbackUrl.StartsWith("/avatar/")) {
                return TypedResults.LocalRedirect(UriHelper.Encode(new Uri(fallbackUrl, UriKind.RelativeOrAbsolute)));
            }
            return TypedResults.NotFound();
        }

        var hash = MD5.HashData(Encoding.UTF8.GetBytes(pictureKey)).ToBase64UrlSafe();
        return TypedResults.File(stream, contentType: contentType, entityTag: new Microsoft.Net.Http.Headers.EntityTagHeaderValue(new Microsoft.Extensions.Primitives.StringSegment($"\"{hash}_{stream.Length}\"")));
    }

    private static string? GetImageContentType(string? fileExtention) {
        if (fileExtention is null) return null;

        var dicSupportedFormats = new Dictionary<string, string> {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp"
        };

        if (dicSupportedFormats.TryGetValue(fileExtention, out var fileType))
            return fileType;

        return null;
    }

}
