using Indice.Features.Cases.Core.Services.Abstractions;

namespace Indice.Features.Cases.Core;

internal class CasesCoreConstants
{
    /// <summary>The default name for systemic claims principal.</summary>
    public static string SystemUserName = "system_user";
    /// <summary>Cases API scope.</summary>
    public const string DefaultScopeName = "cases";

    /// <summary>The default name for the ReferenceNumber sequence.</summary>
    public const string ReferenceNumberSequence = "ReferenceNumberSequence";

    /// <summary>Default database schema.</summary>
    public const string DatabaseSchema = "case";

    /// <summary>The default groupId claim type. Would be <strong>groupId</strong></summary>
    public static string DefaultGroupIdClaimType = "group_id";
    /// <summary>The default contact reference claim type. Would be <strong>customer_code</strong></summary>
    public static string DefaultReferenceIdClaimType = "customer_code";

    /// <summary>The default language key that will be used at <see cref="IJsonTranslationService"/>.</summary>
    public static string DefaultTranslationLanguage = "el";

    /// <summary>Case metadata keys the feature knows about.</summary>
    public static class MetadataKeys
    {
        /// <summary>Correlates cases. Kept equal to the case's <c>RelationKey</c> column, which serves the queries.</summary>
        public const string ExternalCorrelationKey = nameof(ExternalCorrelationKey);
    }

    /// <summary>Cases Api default channels.</summary>
    /// <remarks>Callers may create cases with any other channel name.</remarks>
    public static class Channels
    {
        /// <summary>External channel, the default for the cases that have been created through MyCases interface</summary>
        public const string External = nameof(External);

        /// <summary>Managed channel, the default for the cases that have been created through AdminCases interface</summary>
        public const string Managed = nameof(Managed);

        /// <summary>Legacy name of <see cref="External"/>, still stored on the cases created before the rename.</summary>
        public const string Customer = nameof(Customer);

        /// <summary>Legacy name of <see cref="Managed"/>, still stored on the cases created before the rename.</summary>
        public const string Agent = nameof(Agent);

        /// <summary>Maps a legacy channel name to its current one. Any other name is returned as is.</summary>
        public static string? Normalize(string? channel) => channel switch {
            Agent => Managed,
            Customer => External,
            _ => channel
        };

        /// <summary>The values a case of <paramref name="channel"/> may have stored: the channel and its legacy name, if any.</summary>
        public static string[] StoredValues(string channel) => Normalize(channel) switch {
            Managed => [Managed, Agent],
            External => [External, Customer],
            var other => [other!]
        };
    }
}
