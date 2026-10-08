using Indice.AspNetCore.Features.Recaptcha;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.Routing;

internal static partial class RecaptchaHandlers
{
    /// <summary>Validates a reCAPTCHA v3 token and returns whether v2 fallback is required.</summary>
    internal static async Task<Ok<RecaptchaValidateResponse>> ValidateRecaptcha(
        [FromBody] RecaptchaValidateRequest request,
        IRecaptchaService recaptchaService,
        HttpContext httpContext) {
        if (!recaptchaService.IsEnabled) {
            return TypedResults.Ok(new RecaptchaValidateResponse {
                Success = false,
                RequiresV2 = false,
                SiteKeyV2 = null
            });
        }
        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
        var result = await recaptchaService.ValidateAsync(request.Token, request.Version, remoteIp);
        return TypedResults.Ok(new RecaptchaValidateResponse {
            Success = result.Success,
            RequiresV2 = result.RequiresV2Fallback,
            SiteKeyV2 = result.RequiresV2Fallback ? recaptchaService.SiteKeyV2 : null
        });
    }
}

/// <summary>Request model for reCAPTCHA validation.</summary>
public class RecaptchaValidateRequest
{
    /// <summary>The reCAPTCHA token.</summary>
    public string? Token { get; set; }
    /// <summary>The reCAPTCHA version (v2 or v3). Default is v3.</summary>
    public string Version { get; set; } = "v3";
}
/// <summary>Response model for reCAPTCHA validation.</summary>
public class RecaptchaValidateResponse
{
    /// <summary>Indicates if the reCAPTCHA validation was successful.</summary>
    public bool Success { get; set; }
    /// <summary>Indicates if a reCAPTCHA v2 fallback is required.</summary>
    public bool RequiresV2 { get; set; }
    /// <summary>The site key for reCAPTCHA v2, if a fallback is required.</summary>
    public string? SiteKeyV2 { get; set; }
}