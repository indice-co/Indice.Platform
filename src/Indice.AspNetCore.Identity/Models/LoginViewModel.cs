// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>Login view model.</summary>
    public class LoginViewModel : LoginInputModel
    {
        /// <summary>Allow remember login.</summary>
        public bool AllowRememberLogin { get; set; } = true;
        /// <summary>Enables local logins (if false only external provider list will be available).</summary>
        public bool EnableLocalLogin { get; set; } = true;
        /// <summary>List of external providers.</summary>
        public IEnumerable<ExternalProvider> ExternalProviders { get; set; } = Enumerable.Empty<ExternalProvider>();
        /// <summary>The visible external providers are those form the <see cref="ExternalProviders"/> list that have a display name.</summary>
        public IEnumerable<ExternalProvider> VisibleExternalProviders => ExternalProviders.Where(x => !string.IsNullOrWhiteSpace(x.DisplayName));
        /// <summary>Use this flag to hide the local login form.</summary>
        public bool IsExternalLoginOnly => EnableLocalLogin == false && ExternalProviders?.Count() == 1;
        /// <summary>The scheme to use for external login cookie.</summary>
        public string ExternalLoginScheme => IsExternalLoginOnly ? ExternalProviders?.SingleOrDefault()?.AuthenticationScheme : null;
        /// <summary>A direction to display a different screen when a client asks for the authorize endpoint.</summary>
        /// <remarks>Use the 'operation={operation_name}' query parameter on the authorize endpoint.</remarks>
        public string Operation { get; set; }
        /// <summary>Specifies whether a device (browser) id should be generated.</summary>
        public bool GenerateDeviceId { get; set; }
    }

    /// <summary>Extension methods on <see cref="LoginViewModel"/>.</summary>
    public static class LoginViewModelExtensions
    {
        /// <summary>Decides whether to prompt user for registration instead of login.</summary>
        /// <param name="loginViewModel">Login view model.</param>
        public static bool PromptRegister(this LoginViewModel loginViewModel) => loginViewModel.Operation?.Equals("register", StringComparison.OrdinalIgnoreCase) == true;
    }
}
