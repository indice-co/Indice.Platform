using System;
using System.Collections.Generic;
using Indice.Integration.EGov.Kyc.Enums;
using Indice.Integration.EGov.Kyc.Interfaces;
using Indice.Integration.EGov.Kyc.Models;
using Microsoft.Extensions.Localization;

namespace Indice.Integration.EGov.Kyc.Services
{
    /// <inheritdoc />
    public class KycScopeService : IKycScopeService
    {
        private readonly IStringLocalizer<KycScopeService> _localizer;

        public KycScopeService(IStringLocalizer<KycScopeService> localizer) {
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>
        /// Get eGov KYC Scopes with localized descriptions
        /// </summary>
        public List<KycScope> GetEGovKycScopes(IStringLocalizer localizer = null) {
            var loc = localizer ?? _localizer;
            return new List<KycScope> {
                 new KycScope {
                     Scope = KycScopeType.contactInfo.ToString(),
                     Description = loc["ContactInfoDescription"]
                 },
                 new KycScope {
                     Scope = KycScopeType.identity.ToString(),
                     Description = loc["IdentityDescription"]
                 },
                 new KycScope {
                     Scope = KycScopeType.income.ToString(),
                     Description = loc["IncomeDescription"]
                 },
                 new KycScope {
                     Scope = KycScopeType.professionalActivity.ToString(),
                     Description = loc["ProfessionalActivityDescription"]
                 }
            };
        }
    }
}
