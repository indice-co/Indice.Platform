﻿using System.Reflection;
using Indice.Security;

namespace Indice.Features.Identity.Server;

/// <summary>Constants for IdentityServer API feature.</summary>
public static partial class IdentityEndpoints
{
    /// <summary>Identity API sub-scopes.</summary>
    public static partial class SubScopes
    {
        /// <summary>A scope that allows managing totp.</summary>
        public const string Totp = "identity:totp";
    }

    /// <summary>Identity API policies.</summary>
    public static partial class Policies
    {
        /// <summary>A user must have the 'Admin' flag or own one of the <see cref="BasicRoleNames.Administrator"/> or <see cref="BasicRoleNames.AdminUIAdministrator"/> roles.</summary>
        public const string BeWow = nameof(BeWow);
    }
}
