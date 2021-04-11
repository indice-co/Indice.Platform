using System;
using System.Collections.Generic;
using Indice.Security;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Provides functionality to generate test claim types for development purposes.
    /// </summary>
    internal class InitialRoles<TRole> where TRole : Role, new()
    {
        private static readonly List<TRole> Roles = new() {
            new TRole {
                Id = $"{Guid.NewGuid()}",
                Name = BasicRoleNames.Developer,
                NormalizedName = BasicRoleNames.Developer.ToUpper(),
                Description = "A user that has the role of a software developer."
            }
        };

        /// <summary>
        /// Gets a collection of test claim types.
        /// </summary>
        public static IReadOnlyCollection<TRole> Get() => Roles;
    }
}
