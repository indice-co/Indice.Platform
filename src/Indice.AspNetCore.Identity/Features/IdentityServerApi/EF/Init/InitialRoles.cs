using System;
using System.Collections.Generic;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Provides functionality to generate test claim types for development purposes.
    /// </summary>
    internal class InitialRoles<TRole> where TRole : Role, new()
    {
        private static readonly List<TRole> Roles = new List<TRole> {
            new TRole {
                Id = $"{Guid.NewGuid()}",
                Name = "Administrator",
                NormalizedName = "ADMINISTRATOR",
                Description = "Administrator of Admin UI, which has access to every operation."
            },
            new TRole {
                Id = $"{Guid.NewGuid()}",
                Name = "Developer",
                NormalizedName = "DEVELOPER",
                Description = "A user that has the role of a software developer."
            }
        };

        /// <summary>
        /// Gets a collection of test claim types.
        /// </summary>
        public static IReadOnlyCollection<TRole> Get() => Roles;
    }
}
