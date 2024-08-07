﻿namespace Indice.Features.Identity.UI.Models;

/// <summary>Request input model for the login page.</summary>
public class LoginInputModel
{
    /// <summary>The user name.</summary>
    public string? UserName { get; set; }
    /// <summary>The password</summary>
    public string? Password { get; set; }
    /// <summary>Flag that indicates that the login cookie should be persisted</summary>
    public bool RememberLogin { get; set; }
    /// <summary>The return URL after the login is successful.</summary>
    public string? ReturnUrl { get; set; }
    /// <summary>The id of the current client in the request. </summary>
    public string? ClientId { get; set; }
    /// <summary>Device id generated by the client.</summary>
    public string? DeviceId { get; set; }
}
