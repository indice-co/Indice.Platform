using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.AspNetCore.Features.SignalRProxy;

/// <summary>
/// Provides authentication helpers for SignalR proxying.
/// </summary>
public static class SignalRProxyAuthentication
{
    /// <summary>Defines the authentication scheme for SignalR negotiation.</summary>
    public const string SignalRNegotiationAuthenticationScheme = nameof(SignalRNegotiationAuthenticationScheme);

    /// <summary> The name of the header used to pass the authorization token during SignalR negotiation.</summary>
    public const string SignalRNegotiateAuthorizationHeader = "X-Negotiate-Authorization";

    /// <summary>
    /// Creates a function to retrieve a token from the SignalR negotiate request.
    /// </summary>
    /// <param name="authorizationHeaderName">The name of the authorization header, default is "X-Negotiate-Authorization".</param>
    /// <param name="defaultTokenRetriever">The default function if the authorization header is not found, default is null.</param>
    /// <returns>Returns a function that retrieves the token from the SignalR negotiate request.</returns>
    public static Func<HttpRequest, string?> SignalRNegotiateTokenRetriever(
        string authorizationHeaderName = SignalRNegotiateAuthorizationHeader,
        Func<HttpRequest, string>? defaultTokenRetriever = null) {
        return request => {
            if (request.Headers[authorizationHeaderName] is { Count: > 0 } authorizationHeader) {
                // this is the same as Substring("Bearer".Length + 1).Trim()";
                return authorizationHeader.ToString()[("Bearer".Length + 1)..].Trim();
            }

            return defaultTokenRetriever?.Invoke(request);
        };
    }

    /// <summary>
    /// Adds SignalR Proxy authentication using a policy scheme that forwards authentication and challenge to the specified scheme.
    /// </summary>
    /// <param name="builder">The authentication builder.</param>
    /// <param name="forwardAuthenticate">The authentication scheme to forward to.</param>
    /// <returns></returns>
    public static AuthenticationBuilder AddSignalRProxyAuthentication(this AuthenticationBuilder builder, string forwardAuthenticate = "Bearer") {
        builder.AddPolicyScheme(SignalRNegotiationAuthenticationScheme, "SignalR Negotiate Policy",
                options => {
                    options.ForwardAuthenticate = forwardAuthenticate;
                    options.ForwardChallenge = forwardAuthenticate;
                });
        builder.Services.Configure<SignalRProxyOptions>(options => {
            options.NegotiateAuthenticationSchemes.Add(SignalRNegotiationAuthenticationScheme);
        });
        return builder;
    }
}
