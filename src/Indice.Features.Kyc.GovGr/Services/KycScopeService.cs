using System;
using System.Collections.Generic;
using Indice.Features.Kyc.GovGr.Enums;
using Indice.Features.Kyc.GovGr.Interfaces;
using Indice.Features.Kyc.GovGr.Models;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Kyc.GovGr.Services
{
    /// <inheritdoc />
    public class KycScopeService : IKycScopeService
    {
        private readonly IStringLocalizer<KycScopeService> _localizer;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="localizer"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public KycScopeService(IStringLocalizer<KycScopeService> localizer) {
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <inheritdoc />
        public List<KycScope> GetEGovKycScopes(IStringLocalizer localizer = null) {
            var loc = localizer ?? _localizer;
            return new List<KycScope> {
                 new KycScope {
                     Scope = GovGrKycScopes.ContactInfo,
                     Description = loc["ContactInfoDescription"]
                 },
                 new KycScope {
                     Scope = GovGrKycScopes.Identity,
                     Description = loc["IdentityDescription"]
                 },
                 new KycScope {
                     Scope = GovGrKycScopes.Income,
                     Description = loc["IncomeDescription"]
                 },
                 new KycScope {
                     Scope = GovGrKycScopes.ProfessionalActivity,
                     Description = loc["ProfessionalActivityDescription"]
                 }
            };
        }
    }
}
