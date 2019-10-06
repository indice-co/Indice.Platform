using System;
using System.Collections.Generic;
using Indice.AspNetCore.Identity.Features;

namespace Indice.Identity.Data.Init
{
    /// <summary>
    /// Provides functionality to generate test claim types for development purposes.
    /// </summary>
    public class InitialRoles
    {
        private static readonly List<Role> Roles = new List<Role> {
            new Role {
                Id = $"{Guid.NewGuid()}",
                Name = "Administrator",
                NormalizedName = "ADMINISTRATOR",
                Description = "Administrator of Admin UI, which has access to every operation."
            }
        };

        /// <summary>
        /// Gets a collection of test claim types.
        /// </summary>
        public static IReadOnlyCollection<Role> Get() => Roles;
    }
}
