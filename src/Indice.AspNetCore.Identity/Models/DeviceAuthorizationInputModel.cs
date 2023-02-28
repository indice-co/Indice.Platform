// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


namespace Indice.AspNetCore.Identity.Models;

/// <summary>Device authorization input model (request model)</summary>
public class DeviceAuthorizationInputModel : ConsentInputModel
{
    /// <summary>User code</summary>
    public string UserCode { get; set; }
}