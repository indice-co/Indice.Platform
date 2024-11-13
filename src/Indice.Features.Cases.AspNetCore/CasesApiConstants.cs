using System.Reflection;
using Indice.Features.Cases.Interfaces;

namespace Indice.Features.Cases;

/// <summary>Constant values for Cases Api</summary>
public static class CasesApiConstants
{
    /// <summary>The assembly name.</summary>
    public static readonly string AssemblyName = Assembly.GetExecutingAssembly().GetName().Name!;

    /// <summary>Authentication scheme name used by Cases API.</summary>
    public const string AuthenticationScheme = "Bearer";

    /// <summary>Cases API scope.</summary>
    public const string Scope = "cases-api";

    /// <summary>Cases GroupName Api Explorer.</summary>
    public const string GroupName = "cases-api";

    /// <summary>Default database schema.</summary>
    public const string DatabaseSchema = "case";

    /// <summary>The default language key that will be used at <see cref="IJsonTranslationService"/>.</summary>
    public static string DefaultTranslationLanguage = "el";

    /// <summary>The default groupId claim type.</summary>
    public static string DefaultGroupIdClaimType = "groupId";

    /// <summary>The default name for systemic claims principal.</summary>
    public static string SystemUserName = "system_user";

    /// <summary>The default name for the ReferenceNumber sequence.</summary>
    public const string ReferenceNumberSequence = "ReferenceNumberSequence";

    /// <summary>Cases API policies.</summary>
    public static class Policies
    {
        /// <summary>A user must have the BeCasesManager role to be authorized for AdminCases Feature </summary>
        public const string BeCasesManager = nameof(BeCasesManager);

        /// <summary>A user must have the BeCasesUser role to be authorized for MyCases Feature </summary>
        public const string BeCasesUser = nameof(BeCasesUser);

        /// <summary>A user must have the BeCasesAdministrator role to be authorized for AdminCaseTypes Feature </summary>
        public const string BeCasesAdministrator = nameof(BeCasesAdministrator);
    }

    /// <summary>Cases Api default channels.</summary>
    public static class Channels
    {
        /// <summary>Customer channel, for the cases that have been created through MyCases interface</summary>
        public const string Customer = nameof(Customer);

        /// <summary>Agent channel, for the cases that have been created through AdminCases interface</summary>
        public const string Agent = nameof(Agent);
    }

    /// <summary>Global Workflow variables</summary>
    public static class WorkflowVariables
    {
        /// <summary>The reject reasons for an approval workflow.</summary>
        public const string RejectReasons = nameof(RejectReasons);

        /// <summary>The Custom outcome names for the workflow activities.</summary>
        public static class OutcomeNames
        {
            /// <summary>Outcome for "Failed".</summary>
            public const string Failed = nameof(Failed);

            /// <summary>Outcome for "Save".</summary>
            public const string Save = nameof(Save);
        }
    }

    /// <summary>Cases API keys for validation errors.</summary>
    public static class ValidationErrorKeys
    {
        /// <summary>Indicates that the case attachment file extension is not acceptable.</summary>
        public const string FileExtension = "FILE_EXTENSION";
    }
}

/// <summary>Constant values for Cases Api Groups.</summary>
internal static class ApiGroups
{
    /// <summary>The group name placeholder that will be replaced for admin endpoints.</summary>
    internal const string CasesApiGroupNamePlaceholder = "[casesApiGroupName]";

    /// <summary>The group name placeholder that will be replaced for my-cases endpoints.</summary>
    internal const string MyCasesApiGroupNamePlaceholder = "[myCasesApiGroupName]";
}

/// <summary>Constant values for Cases Api Prefixes.</summary>
internal class ApiPrefixes
{
    /// <summary>The admin api prefix placeholder that will be replaced.</summary>
    internal const string CasesApiTemplatePrefixPlaceholder = "[casesApiPrefix]";

    /// <summary>The my-cases api prefix placeholder that will be replaced.</summary>
    internal const string MyCasesApiTemplatePrefixPlaceholder = "[myCasesApiPrefix]";
}