using System;
using System.Collections.Generic;
using System.Text;

namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>
    /// A user password hashed stored for passwork history validation purposes
    /// </summary>
    public class UserPassword
    {
        /// <summary>
        /// constructs a new instance with a new Guid for ID.
        /// </summary>
        public UserPassword() {
            Id = Guid.NewGuid();
        }

        /// <summary>
        /// The id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The user id related
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Password hash
        /// </summary>
        public string PasswordHash { get; set; }

        /// <summary>
        /// The date this password was created.
        /// </summary>
        public DateTimeOffset DateCreated { get; set; }
    }
}
