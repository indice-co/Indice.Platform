#if NET7_0_OR_GREATER
#nullable enable
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Indice.Extensions;
using Indice.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Net.Http.Headers;

namespace Indice.AspNetCore.Features.Avatar;
internal static class AvatarsHandlers
{

    /// <summary>Creates an avatar using random background color based on full name and optional size.</summary>
    /// <param name="response"></param>
    /// <param name="avatarGenerator"></param>
    /// <param name="fullname">The full name to use when creating the avatar.</param>
    /// <param name="ext">The file extension (.png or .jpg).</param>
    /// <param name="circular">Determines whether the tile will be circular or square. Defaults to false (square)</param>
    /// <returns></returns>
    public static Results<FileStreamHttpResult, NotFound, ValidationProblem> GetAvatar1(HttpResponse response, AvatarGenerator avatarGenerator, string fullname, string? ext, bool? circular) => GetAvatar(response, avatarGenerator, fullname, size: 192, background: null, ext, foreground: null, circular);

    /// <summary>Creates an avatar using random background color based on full name and optional size.</summary>
    /// <param name="response"></param>
    /// <param name="avatarGenerator"></param>
    /// <param name="fullname">The full name to use when creating the avatar.</param>
    /// <param name="size">The desired size of the avatar. Possible values are 16, 24, 32, 48, 64, 128, 192, 256 and 512.</param>
    /// <param name="circular">Determines whether the tile will be circular or square. Defaults to false (square)</param>
    /// <returns></returns>
    public static Results<FileStreamHttpResult, NotFound, ValidationProblem> GetAvatar2(HttpResponse response, AvatarGenerator avatarGenerator, string fullname, int? size, bool? circular) => GetAvatar(response, avatarGenerator, fullname, size ?? 192, background: null, ext: null, foreground: null, circular);

    /// <summary>Creates an avatar using random background color based on full name and optional size and extension.</summary>
    /// <param name="response"></param>
    /// <param name="avatarGenerator"></param>
    /// <param name="fullname">The full name to use when creating the avatar.</param>
    /// <param name="size">The desired size of the avatar. Possible values are 16, 24, 32, 48, 64, 128, 192, 256 and 512.</param>
    /// <param name="ext">The file extension (.png or .jpg).</param>
    /// <param name="circular">Determines whether the tile will be circular or square. Defaults to false (square)</param>
    /// <returns></returns>
    public static Results<FileStreamHttpResult, NotFound, ValidationProblem> GetAvatar3(HttpResponse response, AvatarGenerator avatarGenerator, string fullname, int size, string? ext, bool? circular) => GetAvatar(response, avatarGenerator, fullname, size, background: null, ext, foreground: null, circular);

    /// <summary>Creates an avatar using full name, size, background color and optional extension.</summary>
    /// <param name="response"></param>
    /// <param name="avatarGenerator"></param>
    /// <param name="fullname">The full name to use when creating the avatar.</param>
    /// <param name="size">The desired size of the avatar. Possible values are 16, 24, 32, 48, 64, 128, 192, 256 and 512.</param>
    /// <param name="background">The background color to use.</param>
    /// <param name="ext">The file extension (.png or .jpg).</param>
    /// <param name="foreground">The foreground color to use.</param>
    /// <param name="circular">Determines whether the tile will be circular or square. Defaults to false (square)</param>
    /// <returns></returns>
    public static Results<FileStreamHttpResult, NotFound, ValidationProblem> GetAvatar(HttpResponse response, AvatarGenerator avatarGenerator, string fullname, int size, string? background, string? ext, string? foreground, bool? circular) {
        if (string.IsNullOrWhiteSpace(fullname)) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(fullname), "Fullname cannot be empty"));
        }
        var parts = fullname.Trim().Split(' ');
        var firstName = parts[0];
        var lastName = fullname.Trim().Remove(0, firstName.Length).TrimStart();
        if (parts.Length == 1) {
            if (int.TryParse(firstName, out var number)) {
                firstName = number.ToString();
                lastName = null;
            } else {
                parts = Regex.Split(firstName, @"(?<=\p{L})(?=\p{Lu}\p{Ll})|(?<=[\p{Ll}0-9])(?=[0-9]?\p{Lu})");
                firstName = parts[0];
                lastName = fullname.Trim().Remove(0, firstName.Length).TrimStart();
            }
        }
        return GetAvatarFull(response, avatarGenerator, firstName, lastName, size, ext, background, foreground, circular ?? false, null);
    }

    /// <summary>Creates an avatar based on first and last name plus parameters</summary>
    /// <param name="response"></param>
    /// <param name="avatarGenerator">Avatar generator</param>
    /// <param name="firstName">First name to use.</param>
    /// <param name="lastName">Last name to use.</param>
    /// <param name="size">The desired size of the avatar. Possible values are 16, 24, 32, 48, 64, 128, 192, 256 and 512.</param>
    /// <param name="ext">The file extension (.png or .jpg).</param>
    /// <param name="background">The background color to use.</param>
    /// <param name="foreground">The foreground color to use.</param>
    /// <param name="circular">Determines whether the tile will be circular or square. Defaults to false (square)</param>
    /// <param name="v">Use for cache busting.</param>
    /// <returns></returns>
    public static Results<FileStreamHttpResult, NotFound, ValidationProblem> GetAvatarFull(HttpResponse response, AvatarGenerator avatarGenerator, string? firstName, string? lastName, int? size, string? ext, string? background, string? foreground, bool? circular, string? v) {
        var errors = ValidationErrors.Create();
        if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName)) {
            errors.AddError(nameof(firstName), "Must provide with first or last name.");
        }
        if (size.HasValue && !avatarGenerator.AllowedSizes.Contains((int)size)) {
            errors.AddError(nameof(size), $"Size is out of range. Valid sizes are {string.Join(", ", avatarGenerator.AllowedSizes.OrderBy(x => x))}.");
        }
        if (!string.IsNullOrEmpty(ext) && !new[] { "png", "jpg" }.Contains(ext)) {
            errors.AddError(nameof(size), "Extension is out of range of valid values. Accepts only .png and .jpg.");
        }
        if (errors.Count > 0) {
            return TypedResults.ValidationProblem(errors);
        }
        var data = new MemoryStream();
        avatarGenerator.Generate(data, firstName, lastName, size ?? 192, ext == "jpg", background, foreground, circular ?? false);
        var hash = string.Empty;
        using (var md5 = MD5.Create()) {
            hash = md5.ComputeHash(Encoding.UTF8.GetBytes($"{firstName} {lastName}")).ToBase64UrlSafe();
        }
        response.Headers.Add("Cache-Control", "max-age=345600");
        //var filename = $"{firstName}_{lastName}_{size ?? 192}.{ext}";
        return TypedResults.File(data, 
                                contentType: ext == "jpg" ? "image/jpeg" : "image/png", 
                                lastModified: DateTimeOffset.UtcNow, 
                                entityTag: new EntityTagHeaderValue($"\"{hash}\""));
    }
}
#nullable disable
#endif