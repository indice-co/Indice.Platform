namespace Indice.Features.Cases.Server;

/// <summary>Constant values for Cases Api</summary>
public static class CasesApiConstants
{
    /// <summary>Cases API policies.</summary>
    public static class Policies
    {
        /// <summary>A user must satisfy the BeCasesManager authorization policy to be use the AdminCases Feature </summary>
        public const string BeCasesManager = nameof(BeCasesManager);

        /// <summary>A user must satisfy the BeCasesUser authorization policy to be use the MyCases Feature </summary>
        public const string BeCasesUser = nameof(BeCasesUser);

        /// <summary>A user must satisfy the BeCasesAdministrator authorization policy to be use the AdminCaseTypes Feature </summary>
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
}
