using Indice.Configuration;
using Indice.Features.Cases.Core;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Cases.Server.Endpoints.Validators;

/// <summary>Validates the metadata a case is created or patched with.</summary>
internal static class CaseMetadataValidation
{
    /// <summary>The longest relation key the case table stores.</summary>
    public const int RelationKeyMaxLength = TextSizePresets.M256;

    /// <summary>The error message of an relation key that is too long.</summary>
    public static readonly string RelationKeyTooLong = $"{CasesCoreConstants.MetadataKeys.ExternalCorrelationKey} must be {RelationKeyMaxLength} characters or fewer.";

    /// <summary>Whether the <c>ExternalCorrelationKey</c> entry of the metadata, if any, fits the case table.</summary>
    public static bool IsValid(Dictionary<string, string>? metadata) =>
        metadata?.GetValueOrDefault(CasesCoreConstants.MetadataKeys.ExternalCorrelationKey)?.Length <= RelationKeyMaxLength;

    /// <summary>The validation errors of the metadata, or <see langword="null"/> when it is valid.</summary>
    public static IDictionary<string, string[]>? Validate(Dictionary<string, string>? metadata) =>
        IsValid(metadata) ? null : ValidationErrors.AddError($"metadata.{CasesCoreConstants.MetadataKeys.ExternalCorrelationKey}", RelationKeyTooLong);
}
