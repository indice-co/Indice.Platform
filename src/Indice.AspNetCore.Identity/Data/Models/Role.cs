using Indice.Security;
using Microsoft.AspNetCore.Identity;

namespace Indice.AspNetCore.Identity.Data.Models
{
    /// <summary>
    /// Extended version of the <see cref="IdentityRole"/> class.
    /// </summary>
    public class Role : IdentityRole
    {
        /// <summary>
        /// Creates a new instance of <see cref="Role"/>.
        /// </summary>
        public Role() : this(string.Empty) { }

        /// <summary>
        /// Creates a new instance of <see cref="Role"/>.
        /// </summary>
        /// <param name="roleName">The name of the role.</param>
        public Role(string roleName) : base(roleName) { }

        /// <summary>
        /// A description for the role.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Checks if the specified role is one of management roles.
        /// </summary>
        /// <param name="role">The role to check.</param>
        public virtual bool IsManagementRole() => 
            Name is BasicRoleNames.Administrator or BasicRoleNames.AdminUIAdministrator or BasicRoleNames.AdminUIUsersReader or BasicRoleNames.AdminUIUsersWriter or BasicRoleNames.AdminUIClientsReader or BasicRoleNames.AdminUIClientsWriter;
    }
}
