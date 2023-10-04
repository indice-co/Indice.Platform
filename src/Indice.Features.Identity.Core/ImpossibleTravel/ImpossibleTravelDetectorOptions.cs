namespace Indice.Features.Identity.Core.ImpossibleTravel;

/// <summary>Configuration options for impossible travel detector feature.</summary>
public class ImpossibleTravelDetectorOptions
{
    /// <summary>The speed (km/h) used to compare the travel speed between two login attempts. Default is 80 km/h.</summary>
    public double AcceptableSpeed { get; set; } = 80d;
    /// <summary>Specifies the flow to follow when impossible travel is detected for the current user. Defaults to <see cref="OnImpossibleTravelFlowType.PromptMfa"/>.</summary>
    public OnImpossibleTravelFlowType OnImpossibleTravelFlowType { get; set; } = OnImpossibleTravelFlowType.PromptMfa;
}
