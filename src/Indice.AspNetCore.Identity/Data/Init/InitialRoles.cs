using System;
using System.Collections.Generic;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.Security;

namespace Indice.AspNetCore.Identity.Data
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
            },
            new TRole {
                Id = $"{Guid.NewGuid()}",
                Name = BasicRoleNames.Administrator,
                NormalizedName = BasicRoleNames.Administrator.ToUpper(),
                Description = "A user that is a system administrator."
            },
            new TRole {
                Id = $"{Guid.NewGuid()}",
                Name = BasicRoleNames.AdminUIAdministrator,
                NormalizedName = BasicRoleNames.AdminUIAdministrator.ToUpper(),
                Description = "A user that can manage users, roles, clients and resources in the Admin UI tool."
            },
            new TRole {
                Id = $"{Guid.NewGuid()}",
                Name = BasicRoleNames.AdminUIUsersReader,
                NormalizedName = BasicRoleNames.AdminUIUsersReader.ToUpper(),
                Description = "A user that can apply read operations on users and roles in the Admin UI tool."
            },
            new TRole {
                Id = $"{Guid.NewGuid()}",
                Name = BasicRoleNames.AdminUIUsersWriter,
                NormalizedName = BasicRoleNames.AdminUIUsersWriter.ToUpper(),
                Description = "A user that can apply read & write operations on users and roles in the Admin UI tool."
            },
            new TRole {
                Id = $"{Guid.NewGuid()}",
                Name = BasicRoleNames.AdminUIClientsReader,
                NormalizedName = BasicRoleNames.AdminUIClientsReader.ToUpper(),
                Description = "A user that can apply read operations on clients and resources in the Admin UI tool."
            },
            new TRole {
                Id = $"{Guid.NewGuid()}",
                Name = BasicRoleNames.AdminUIClientsWriter,
                NormalizedName = BasicRoleNames.AdminUIClientsWriter.ToUpper(),
                Description = "A user that can apply read & write operations on clients in the Admin UI tool."
            }
        };

        /// <summary>
        /// Gets a collection of test claim types.
        /// </summary>
        public static IReadOnlyCollection<TRole> Get() => Roles;
    }
}
