using System;

namespace Indice.AspNetCore.Identity
{
    /// <summary></summary>
    public sealed class SecurityTokenBasedTotpService : TotpServiceBase
    {
        /// <summary>Creates a new instance of <see cref="SecurityTokenBasedTotpService"/>.</summary>
        /// <param name="serviceProvider">Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</param>
        public SecurityTokenBasedTotpService(IServiceProvider serviceProvider) : base(serviceProvider) { }
    }
}
