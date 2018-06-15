using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indice.Security
{
    /// <summary>
    /// Represent a tenant construct for multitenat Applications
    /// </summary>
    public interface ITenant
    {
        /// <summary>
        /// The tenant identifier
        /// </summary>
        Guid Id { get; set; }
    }

    /// <summary>
    /// Represent a tenant construct for multitenat Applications that has an alternate key named alias
    /// </summary>
    public interface ITenantWithAlias : ITenant
    {
        /// <summary>
        /// The tenant identifier
        /// </summary>
        string Alias { get; set; }
    }
}
