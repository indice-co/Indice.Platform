// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


namespace Indice.AspNetCore.Identity.Models;

/// <summary>Viewmodel for the external provider</summary>
public class ExternalProvider
{
    /// <summary>The display name</summary>
    public string DisplayName { get; set; }

    /// <summary>The authentication scheme for the cookie</summary>
    public string AuthenticationScheme { get; set; }
}