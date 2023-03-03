// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
namespace Indice.AspNetCore.Identity.Models;

/// <summary>View model for the loggedout page</summary>
public class LoggedOutViewModel
{
    /// <summary>The configured PostLogout url for the client being logged out.</summary>
    public string PostLogoutRedirectUri { get; set; }

    /// <summary>The client id</summary>
    public string ClientId { get; set; }

    /// <summary>The name associated with the client</summary>
    public string ClientName { get; set; }

    /// <summary>the SignoutIFrameurl</summary>
    public string SignOutIframeUrl { get; set; }

    /// <summary>Setting to skip the loggedout page.</summary>
    public bool AutomaticRedirectAfterSignOut { get; set; } = false;

    /// <summary>The Logut id </summary>
    public string LogoutId { get; set; }

    /// <summary>If this is an external login trigger the external signout accordingly</summary>
    public bool TriggerExternalSignout => ExternalAuthenticationScheme != null;

    /// <summary>The external authenitcation scheme if any</summary>
    public string ExternalAuthenticationScheme { get; set; }
}