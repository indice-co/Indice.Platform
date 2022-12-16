using System.Collections.Generic;
using System.Threading.Tasks;
using Indice.Features.GovGr.Models;
using Microsoft.Extensions.Localization;

namespace Indice.Features.GovGr.Interfaces
{
    /// <summary>The kyc integration service.</summary>
    public interface IKycService
    {
        /// <summary>
        /// Get Data from eGov KYC
        /// </summary>
        Task<KycPayload> GetDataAsync(string code);
        /// <summary>
        /// Get available scopes for the KYC endpoint
        /// </summary>
        List<ScopeDescription> GetAvailableScopes(IStringLocalizer localizer = null);
    }
}
