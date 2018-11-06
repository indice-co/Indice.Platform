using System;
using System.Collections.Generic;
using System.Text;

namespace Indice.AspNetCore.Identity.Authorization
{
    /// <summary>
    /// Basic set of Authorization policy names
    /// </summary>
    public class BasicPolicyNames
    {
        /// <summary>
        /// Only a user marked as Admin in the User store or with a role assignment of the name Administrator
        /// is allowed.
        /// </summary>
        public const string BeAdmin = nameof(BeAdmin);
    }
}
