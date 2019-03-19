// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;

namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>
    /// Account options used to configure account behavior.
    /// </summary>
    public class AccountOptions
    {
        public static bool AllowLocalLogin = true;
        public static bool AllowRememberLogin = true;
        public static TimeSpan RememberMeLoginDuration = TimeSpan.FromDays(30);

        public static bool ShowLogoutPrompt = true;
        public static bool AutomaticRedirectAfterSignOut = false;

        /// <summary>
        /// specify the Windows authentication scheme being used. Windows authentication scheme and display name.
        /// </summary>
        public static readonly string WindowsAuthenticationSchemeName = Microsoft.AspNetCore.Server.IISIntegration.IISDefaults.AuthenticationScheme;
        /// <summary>
        /// if user uses windows auth, should we load the groups from windows
        /// </summary>
        public static bool IncludeWindowsGroups = false;

        public static string InvalidCredentialsErrorMessage = "Invalid username or password";

        /// <summary>
        /// Error message when user is not enabled.
        /// </summary>
        public static string NotEnabledErrorMessage = "Your account is not enabled.";
    }
}
