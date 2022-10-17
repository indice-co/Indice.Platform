using System.Collections.Generic;
using Indice.Features.Kyc.GovGr.Models;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Kyc.GovGr.Interfaces
{
    /// <summary>
    /// The Kyc Scopes Service.
    /// </summary>
    public interface IKycScopeService
    {
        /// <summary>
        /// Get eGov KYC Scopes with localized descriptions
        /// </summary>
        List<KycScope> GetEGovKycScopes(IStringLocalizer localizer = null);
    }
}