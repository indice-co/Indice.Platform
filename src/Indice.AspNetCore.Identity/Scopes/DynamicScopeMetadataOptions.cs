using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Indice.AspNetCore.Identity.Scopes
{
    /// <summary>
    /// Dynamic scope metadata configuration options.
    /// </summary>
    public class DynamicScopeMetadataOptions
    {
        /// <summary>
        /// The http url to the metadata endpoint.
        /// </summary>
        public string Endpoint { get; set; }
    }
}
