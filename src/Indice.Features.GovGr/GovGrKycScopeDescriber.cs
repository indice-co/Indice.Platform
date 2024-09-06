﻿using Indice.Features.GovGr.Models;
using Microsoft.Extensions.Localization;

namespace Indice.Features.GovGr;

/// <inheritdoc />
public class GovGrKycScopeDescriber
{
    private readonly IStringLocalizer<GovGrKycScopeDescriber> _localizer;

    /// <summary>constructor</summary>
    /// <param name="localizer"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public GovGrKycScopeDescriber(IStringLocalizer<GovGrKycScopeDescriber> localizer) {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    /// <inheritdoc />
    public List<ScopeDescription> GetDescriptions(IStringLocalizer localizer = null) {
        var loc = localizer ?? _localizer;
        return new List<ScopeDescription> {
             new ScopeDescription {
                 Scope = GovGrKycScopes.ContactInfo,
                 Description = loc["ContactInfoDescription"]
             },
             new ScopeDescription {
                 Scope = GovGrKycScopes.Identity,
                 Description = loc["IdentityDescription"]
             },
             new ScopeDescription {
                 Scope = GovGrKycScopes.Income,
                 Description = loc["IncomeDescription"]
             },
             new ScopeDescription {
                 Scope = GovGrKycScopes.ProfessionalActivity,
                 Description = loc["ProfessionalActivityDescription"]
             },
             new ScopeDescription {
                 Scope = GovGrKycScopes.TaxIdInfo,
                 Description = loc["TaxIdInfoDescription"]
             },
             new ScopeDescription {
                 Scope = GovGrKycScopes.FamilyStatus,
                 Description = loc["FamilyStatusDescription"]
             },
            new ScopeDescription {
                 Scope = GovGrKycScopes.Demographics,
                 Description = loc["DemographicsDescription"]
             },
        };
    }
}
