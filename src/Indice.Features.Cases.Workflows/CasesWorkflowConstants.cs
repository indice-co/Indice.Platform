namespace Indice.Features.Cases.Workflows;
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
