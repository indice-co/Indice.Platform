using System.Collections.Generic;
using Indice.Integration.EGov.Kyc.Models;
using Microsoft.Extensions.Localization;

namespace Indice.Integration.EGov.Kyc.Interfaces
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