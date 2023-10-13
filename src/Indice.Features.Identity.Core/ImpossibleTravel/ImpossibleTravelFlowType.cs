namespace Indice.Features.Identity.Core.ImpossibleTravel;

/// <summary>Specifies the flow to follow when impossible travel is detected for the current user.</summary>
public enum ImpossibleTravelFlowType
{
    /// <summary>Prompts the MFA flow when applicable.</summary>
    PromptMfa,
    /// <summary>Does not allow the user to login.</summary>
    DenyLogin
}