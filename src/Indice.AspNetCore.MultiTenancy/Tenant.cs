using System;
using System.Collections.Generic;

namespace Indice.AspNetCore.MultiTenancy
{
    /* attribution https://michael-mckenna.com/multi-tenant-asp-dot-net-core-application-tenant-resolution */
    /// <summary>
    /// Tenant information
    /// </summary>
    public class Tenant
    {
        /// <summary>
        /// The tenant Id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The tenant identifier
        /// </summary>
        public string Identifier { get; set; }

        /// <summary>
        /// The connection string to the current tenant backing store (database)
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Tenant items
        /// </summary>
        public Dictionary<string, object> Items { get; private set; } = new Dictionary<string, object>();
    }
}
