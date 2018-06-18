using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>
    /// Represents a user in the Identity System
    /// </summary>
    public class User : IdentityUser
    {
        
        /// <summary>
        /// Initializes a new instance of <see cref="IdentityUser"/>.
        /// </summary>
        public User() : this(string.Empty, Guid.NewGuid()) { }

        /// <summary>
        /// Initializes a new instance of <see cref="IdentityUser"/>.
        /// </summary>
        /// <remarks>The Id property is initialized to from a new GUID string value.</remarks>
        /// <param name="userName">The user name</param>
        public User(string userName) : base(userName) { }

        /// <summary>
        /// Initializes a new instance of <see cref="IdentityUser"/>.
        /// </summary>
        public User(string userName, Guid id) : base(userName) {
            Id = id.ToString();
            UserName = userName;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="IdentityUser"/>.
        /// </summary>
        public User(string userName, string id) : base(userName) {
            Id = id;
            UserName = userName;
        }

        /// <summary>
        /// Date that the user was registered
        /// </summary>
        public DateTime RegisteredAt { get; set; }

        /// <summary>
        /// System Administrator Indicator
        /// </summary>
        public bool Admin { get; set; }

        /// <summary>
        /// Navigation property for the roles this user belongs to.
        /// </summary>
        public virtual ICollection<IdentityUserRole<string>> Roles { get; } = new List<IdentityUserRole<string>>();

        /// <summary>
        /// Navigation property for the claims this user possesses.
        /// </summary>
        public virtual ICollection<IdentityUserClaim<string>> Claims { get; } = new List<IdentityUserClaim<string>>();

        /// <summary>
        /// Navigation property for this users login accounts.
        /// </summary>
        public virtual ICollection<IdentityUserLogin<string>> Logins { get; } = new List<IdentityUserLogin<string>>();
    }
}
