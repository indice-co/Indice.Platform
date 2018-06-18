// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using System;

namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>
    /// Account options used to configure account behavior
    /// </summary>
    public static class AccountOptions
    {
        public static bool AllowLocalLogin = true;
        public static bool AllowRememberLogin = true;
        public static TimeSpan RememberMeLoginDuration = TimeSpan.FromDays(30);
        public static bool ShowLogoutPrompt = true;
        public static bool AutomaticRedirectAfterSignOut = false;
        // To enable windows authentication, the host (IIS or IIS Express) also must have windows auth enabled.
        public static bool WindowsAuthenticationEnabled = true;
        public static bool IncludeWindowsGroups = false;
        // Specify the Windows authentication scheme and display name.
        public static readonly string WindowsAuthenticationSchemeName = "Windows";
        public static string InvalidCredentialsErrorMessage = "Invalid username or password.";
    }
}
