// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;

namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>
    /// Account options used to configure account behavior.
    /// </summary>
    public static class AccountOptions
    {
        /// <summary>
        /// Allow local login or only external.
        /// </summary>
        public static bool AllowLocalLogin = true;
        /// <summary>
        /// Allow remember me in login screen.
        /// </summary>
        public static bool AllowRememberLogin = true;
        /// <summary>
        /// Remember me duration.
        /// </summary>
        public static TimeSpan RememberMeLoginDuration = TimeSpan.FromDays(30);
        /// <summary>
        /// Should show the logout prompt or logout immediately.
        /// </summary>
        public static bool ShowLogoutPrompt = true;
        /// <summary>
        /// Automatic redirect after sign out.
        /// </summary>
        public static bool AutomaticRedirectAfterSignOut = true;
        /// <summary>
        /// Invalid username or password.
        /// </summary>
        public static string InvalidCredentialsErrorMessage = "Invalid username or password";
        /// <summary>
        /// Error message when user is not enabled.
        /// </summary>
        public static string NotEnabledErrorMessage = "Your account is not enabled.";
    }
}
