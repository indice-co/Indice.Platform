// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Indice.AspNetCore.Identity.Models;

/// <summary>Logout view model.</summary>
public class LogoutViewModel : LogoutInputModel
{
    /// <summary>Should show the prompt or auto logout?</summary>
    public bool ShowLogoutPrompt { get; set; } = true;
}
