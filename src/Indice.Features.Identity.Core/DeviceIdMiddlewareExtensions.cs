using System.Security.Claims;
using IdentityModel;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Indice.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Builder;

/// <summary>Contains extension methods on <see cref="IApplicationBuilder"/> type.</summary>
public static class DeviceIdMiddlewareExtensions
{
    /// <summary>A middleware that tries to inspect the device identifier that performed the initial login and add it in the user token.</summary>
    /// <param name="app">Defines a class that provides the mechanisms to configure an application's request pipeline.</param>
    /// <remarks>It is crucial to register this middleware before <see cref="IdentityServerApplicationBuilderExtensions.UseIdentityServer(IApplicationBuilder, IdentityServerMiddlewareOptions)"/>.</remarks>
    public static void UseDeviceId(this IApplicationBuilder app) => app.Use(async (httpContext, next) => {
        var request = httpContext.Request;
        if (request.Path.Equals("/connect/token", StringComparison.OrdinalIgnoreCase)) {
            if (request.HasFormContentType && request.Form.TryGetValue(OidcConstants.TokenRequest.Code, out var authorizationCodeValue)) {
                var authorizationCodeStore = httpContext.RequestServices.GetRequiredService<IAuthorizationCodeStore>();
                var authorizationCode = await authorizationCodeStore.GetAuthorizationCodeAsync(authorizationCodeValue);
                if (authorizationCode is not null) {
                    var deviceId = authorizationCode.Subject.FindFirstValue(BasicClaimTypes.DeviceId);
                    if (!string.IsNullOrWhiteSpace(deviceId)) {
                        httpContext.Items.TryAdd("deviceId", deviceId);
                    }
                }
            }
        }
        if (request.Path.Equals("/connect/userinfo", StringComparison.OrdinalIgnoreCase)) {
            if (request.Headers.TryGetValue(HeaderNames.Authorization, out var authorizationHeaderValue)) {
                var tokenValidator = httpContext.RequestServices.GetRequiredService<ITokenValidator>();
                authorizationHeaderValue = authorizationHeaderValue.ToString().Replace("Bearer ", string.Empty);
                var tokenValidationResult = await tokenValidator.ValidateAccessTokenAsync(authorizationHeaderValue);
                if (tokenValidationResult?.IsError == false) {
                    var deviceId = tokenValidationResult.Claims.FirstOrDefault(x => x.Type == BasicClaimTypes.DeviceId)?.Value;
                    if (!string.IsNullOrWhiteSpace(deviceId)) {
                        httpContext.Items.TryAdd("deviceId", deviceId);
                    }
                }
            }
        }
        await next.Invoke(httpContext);
    });
}
