using Indice.Security;
using Microsoft.AspNetCore.Identity;

namespace Indice.Features.Identity.Core.Data.Models;

/// <summary>Extended version of the <see cref="IdentityRole"/> class.</summary>
public class DbRole : IdentityRole
{
    /// <summary>Creates a new instance of <see cref="DbRole"/>.</summary>
    public DbRole() : this(string.Empty) { }

    /// <summary>Creates a new instance of <see cref="DbRole"/>.</summary>
    /// <param name="roleName">The name of the role.</param>
    public DbRole(string roleName) : base(roleName) { }

    /// <summary>A description for the role.</summary>
    public string Description { get; set; }

    /// <summary>Checks if the specified role is one of management roles.</summary>
    public virtual bool IsManagementRole() =>
        Name is BasicRoleNames.Administrator or BasicRoleNames.AdminUIAdministrator or BasicRoleNames.AdminUIUsersReader or BasicRoleNames.AdminUIUsersWriter or BasicRoleNames.AdminUIClientsReader or BasicRoleNames.AdminUIClientsWriter;
}
