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

    /// <summary>Cases Api default channels.</summary>
    public static class Channels
    {
        /// <summary>Customer channel, for the cases that have been created through MyCases interface</summary>
        public const string Customer = nameof(Customer);

        /// <summary>Agent channel, for the cases that have been created through AdminCases interface</summary>
        public const string Agent = nameof(Agent);
    }
}
