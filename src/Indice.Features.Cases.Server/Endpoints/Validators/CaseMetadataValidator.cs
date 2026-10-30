using System.Data;
using FluentValidation;
using Indice.Configuration;
using Indice.Features.Cases.Core;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Cases.Server.Endpoints.Validators;

/// <summary>Validates the metadata a case is created or patched with.</summary>
internal class CaseMetadataValidator : AbstractValidator<Dictionary<string, string>> 
{
    public CaseMetadataValidator() {
        RuleFor(x => x).Must(HaveValidRelationKey).WithMessage(RelationKeyTooLong);
    }

    /// <summary>The longest relation key the case table stores.</summary>
    public const int RelationKeyMaxLength = TextSizePresets.M256;

    /// <summary>The error message of an relation key that is too long.</summary>
    public static readonly string RelationKeyTooLong = $"{CasesCoreConstants.MetadataKeys.ExternalCorrelationKey} must be {RelationKeyMaxLength} characters or fewer.";


    /// <summary>Whether the <c>ExternalCorrelationKey</c> entry of the metadata, if any, fits the case table.</summary>
    public static bool HaveValidRelationKey(Dictionary<string, string>? metadata) {
        if (metadata is null) {
            return true;
        }
        if (metadata.TryGetValue(CasesCoreConstants.MetadataKeys.ExternalCorrelationKey, out var relationkey) && !string.IsNullOrWhiteSpace(relationkey)) {
            return relationkey.Length <= RelationKeyMaxLength;
        }
        return true;
    }

}
