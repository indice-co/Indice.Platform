using Microsoft.AspNetCore.Identity;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Extended version of the <see cref="IdentityRole"/> class.
    /// </summary>
    public sealed class Role : IdentityRole
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
    }
}
