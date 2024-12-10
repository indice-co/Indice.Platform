// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Text.Json;
using Indice.AspNetCore.Authentication.Apple;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods to configure Apple OAuth authentication.</summary>
public static class AppleExtensions
{

    /// <summary>
    /// Adds Apple OAuth-based authentication to <see cref="AuthenticationBuilder"/> using the default scheme.
    /// The default scheme is specified by <see cref="AppleDefaults.AuthenticationScheme"/>.
    /// <para>
    /// Apple authentication allows application users to sign in with their Apple account.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="OpenIdConnectOptions"/>.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddAppleID(this AuthenticationBuilder builder, Action<AppleOptions> configureOptions) => builder.AddAppleID(AppleDefaults.AuthenticationScheme, configureOptions);

    /// <summary>
    /// Adds Apple OAuth-based authentication to <see cref="AuthenticationBuilder"/> using the default scheme.
    /// The default scheme is specified by <see cref="AppleDefaults.AuthenticationScheme"/>.
    /// <para>
    /// Apple authentication allows application users to sign in with their Apple account.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="authenticationScheme">The authentication scheme.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="OpenIdConnectOptions"/>.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddAppleID(this AuthenticationBuilder builder, string authenticationScheme, Action<AppleOptions> configureOptions) => builder.AddAppleID(authenticationScheme, AppleDefaults.DisplayName, configureOptions);

    /// <summary>
    /// Adds Apple OAuth-based authentication to <see cref="AuthenticationBuilder"/> using the default scheme.
    /// The default scheme is specified by <see cref="AppleDefaults.AuthenticationScheme"/>.
    /// <para>
    /// Apple authentication allows application users to sign in with their Apple account.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="authenticationScheme">The authentication scheme.</param>
    /// <param name="displayName">A display name for the authentication handler.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="OpenIdConnectOptions"/>.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddAppleID(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<AppleOptions> configureOptions)
        => builder.AddOpenIdConnect(authenticationScheme, displayName, (options) => {
            var appleOptions = new AppleOptions() {
                CallbackPath = "/signin-apple",
                SignInScheme = "cookie"
            };
            configureOptions?.Invoke(appleOptions);
            if (string.IsNullOrWhiteSpace(appleOptions.TeamId)) {
                throw new ArgumentOutOfRangeException(nameof(appleOptions.TeamId), "Apple ID. The '{0}' option must be provided.");
            }
            if (string.IsNullOrWhiteSpace(appleOptions.ServiceId)) {
                throw new ArgumentOutOfRangeException(nameof(appleOptions.ServiceId), "Apple ID. The '{0}' option must be provided.");
            }
            if (string.IsNullOrWhiteSpace(appleOptions.PrivateKey)) {
                throw new ArgumentOutOfRangeException(nameof(appleOptions.PrivateKey), "Apple ID. The '{0}' option must be provided.");
            }
            if (string.IsNullOrWhiteSpace(appleOptions.PrivateKeyId)) {
                throw new ArgumentOutOfRangeException(nameof(appleOptions.PrivateKeyId), "Apple ID. The '{0}' option must be provided.");
            }
            options.Authority = AppleDefaults.Authority; // Discovery document: https://appleid.apple.com/.well-known/openid-configuration
            options.CallbackPath = appleOptions.CallbackPath; // Corresponding to your redirect URI.
            options.SignInScheme = appleOptions.SignInScheme;
            options.ResponseType = "code id_token"; // Hybrid flow due to lack of PKCE support.
            options.DisableTelemetry = true;
            options.Scope.Clear(); // Apple does not support the profile scope.
            options.Scope.Add("openid");
            options.Scope.Add("email");
            options.Scope.Add("name");
            options.ClientId = appleOptions.ServiceId;
            options.Events = appleOptions.Events;
            // Custom client secret generation - secret can be re-used for up to 6 months.
            options.Events.OnAuthorizationCodeReceived = context => {
                context.TokenEndpointRequest!.ClientSecret = AppleTokenGenerator.CreateNewToken(appleOptions.TeamId, context.Options.Authority!, context.Options.ClientId!, appleOptions.PrivateKey, appleOptions.PrivateKeyId);
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToIdentityProviderForSignOut = context => {
                context.HandleResponse(); // Apple does not support EndSessionEndpoint.
                return Task.CompletedTask;
            };
            // Handle redirect to identity provider for prompt parameter.
            options.Events.OnRedirectToIdentityProvider ??= context => {
                if (context.Properties.Items.TryGetValue("prompt", out var prompt)) {
                    context.ProtocolMessage.Prompt = prompt;
                }
                return Task.CompletedTask;
            };
            options.Events.OnTokenResponseReceived ??= OnTokenResponseReceivedExtractUserClaims;
            options.UsePkce = false; // Apple does not currently support PKCE (April 2021).
        });



    private static Task OnTokenResponseReceivedExtractUserClaims(TokenResponseReceivedContext context) {
        var userText = context.Request.Form["user"].FirstOrDefault();
        if (userText is not null) {
            var claims = ExtractClaimsFromUser(JsonSerializer.Deserialize<JsonElement>(userText));
            var enrichedPrincipal = new ClaimsPrincipal(new ClaimsIdentity(context.Principal!.Identity, claims, context.Principal!.Identity!.AuthenticationType, "name", "role"));
            context.Principal = enrichedPrincipal;
        }
        return Task.CompletedTask;
    }

    /// <summary>Extracts the claims from the user received from the authorization endpoint.</summary>
    /// <param name="user">The user object to extract the claims from.</param>
    /// <returns>
    /// An <see cref="IEnumerable{Claim}"/> containing the claims extracted from the user information.
    /// </returns>
    private static IEnumerable<Claim> ExtractClaimsFromUser(JsonElement user) {
        var claims = new List<Claim>();

        if (user.TryGetProperty("name", out var name)) {
            claims.Add(new Claim("given_name", name.GetString("firstName") ?? string.Empty, "string"));
            claims.Add(new Claim("family_name", name.GetString("lastName") ?? string.Empty, "string"));
        }

        if (user.TryGetProperty("email", out var email)) {
            claims.Add(new Claim("email", email.GetString() ?? string.Empty, "string"));
        }

        return claims;
    }
}
