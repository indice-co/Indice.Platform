namespace Indice.Features.Cases.Server;

internal static class CaseServerConstants
{

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
}
