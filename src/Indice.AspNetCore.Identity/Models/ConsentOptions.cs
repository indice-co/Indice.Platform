// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>
    /// Consent options
    /// </summary>
    public class ConsentOptions
    {
        /// <summary>
        /// Setting for enabling offline access
        /// </summary>
        public static bool EnableOfflineAccess = true;

        /// <summary>
        /// Offline access permission display name
        /// </summary>
        public static string OfflineAccessDisplayName = "Offline Access";
        /// <summary>
        /// Offline access permission description
        /// </summary>
        public static string OfflineAccessDescription = "Access to your applications and resources, even when you are offline";

        /// <summary>
        /// You must pick at least one permission error text
        /// </summary>
        public static readonly string MustChooseOneErrorMessage = "You must pick at least one permission";

        /// <summary>
        /// Invalid selection error text
        /// </summary>
        public static readonly string InvalidSelectionErrorMessage = "Invalid selection";

        /// <summary>
        /// You must pick a strong customer authentication method.
        /// </summary>
        public static readonly string MustChooseOneScaMethod = "You must pick a strong customer authentication method.";

        /// <summary>
        /// You must enter a valid authentication code.
        /// </summary>
        public static readonly string MustEnterAuthenticationCode = "You must enter a valid authentication code.";

        /// <summary>
        /// Invalid authentication code.
        /// </summary>
        public static readonly string InvalidAuthenticationCode = "Invalid authentication code.";
    }
}
