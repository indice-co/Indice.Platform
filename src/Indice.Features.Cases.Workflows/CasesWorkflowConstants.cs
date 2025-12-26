namespace Indice.Features.Cases.Workflows;
/// <summary>
/// Provides a collection of constants used in the Cases Workflow API, including default values,  channel identifiers,
/// workflow variables, and validation error keys.
/// </summary>
/// <remarks>This class is designed to centralize constants for use across the Cases Workflow API,  ensuring
/// consistency and reducing hardcoded values in the application. It includes nested  classes to organize constants by
/// category, such as channels, workflow variables, and validation errors.</remarks>
public static class CasesWorkflowConstants
{

    /// <summary>The default language key that will be used at to translate data.</summary>
    public static string DefaultTranslationLanguage = "el";

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

        /// <summary>The <see cref="Actor"/> of the workflow.</summary>
        public static class Actor
        {
            /// <summary>The Actor that initiated the workflow.</summary>
            public const string Initiator = nameof(Initiator);
            /// <summary>The current Actor aka last actor acting on the workflow.</summary>
            public const string Current = nameof(Current);
        }
    }

    /// <summary>Cases API keys for validation errors.</summary>
    public static class ValidationErrorKeys
    {
        /// <summary>Indicates that the case attachment file extension is not acceptable.</summary>
        public const string FileExtension = "FILE_EXTENSION";
    }
}
