// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Indice.AspNetCore.Authentication.Apple;

/// <summary>Default values for Apple authentication.</summary>
/// <remarks>https://developer.apple.com/documentation/sign_in_with_apple/sign_in_with_apple_rest_api</remarks>
public static class AppleDefaults
{
    /// <summary>The default scheme for Apple authentication. Defaults to <c>Apple</c>.</summary>
    public const string AuthenticationScheme = "Apple";
    /// <summary>The default display name for Apple authentication. Defaults to <c>Sign in with Apple</c>.</summary>
    public static readonly string DisplayName = "Sign in with Apple";
    /// <summary>The default endpoint used to perform Apple authentication.</summary>
    /// <remarks>
    /// For more details about this endpoint, see https://developer.apple.com/documentation/sign_in_with_apple/sign_in_with_apple_rest_api/authenticating_users_with_sign_in_with_apple
    /// </remarks>
    public static readonly string AuthorizationEndpoint = "https://appleid.apple.com/auth/authorize";
    /// <summary>The OAuth endpoint used to exchange access tokens.</summary>
    /// <remarks>
    /// For more details about this endpoint, see https://developer.apple.com/documentation/sign_in_with_apple/generate_and_validate_tokens
    /// </remarks>
    public static readonly string TokenEndpoint = "https://appleid.apple.com/auth/token";
    /// <summary>The OAuth endpoint used to discover OpenId connect supported features.</summary>
    public static readonly string DiscoveryEndpoint = "https://appleid.apple.com/.well-known/openid-configuration";
    /// <summary>The authority.</summary>
    /// <remarks>
    /// For more details about this endpoint, see https://appleid.apple.com/.well-known/openid-configuration
    /// </remarks>
    public static readonly string Authority = "https://appleid.apple.com";
    /// <summary>No user info endpoint is provided, which means all of the claims about users have to be included in the (expiring and potentially large) id_token.</summary>
    public static readonly string UserInformationEndpoint = string.Empty;
}
