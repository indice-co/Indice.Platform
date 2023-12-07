using Indice.Features.Identity.Core.ImpossibleTravel;

namespace Indice.Features.Identity.SignInLogs;

/// <summary>Configuration options regarding impossible travel.</summary>
public class ImpossibleTravelOptions
{
    /// <summary>Default value for <see cref="Guard"/> property.</summary>
    public const bool DEFAULT_IMPOSSIBLE_TRAVEL_GUARD = false;
    /// <summary>Determines whether impossible travel detection is enabled. Defaults to <i>false</i>.</summary>
    /// <remarks>
    /// SignInLogs feature must also be enabled for this to take effect.<br />
    /// If not set, then the <b>IdentityServer:Features:ImpossibleTravel</b> application setting is used.
    /// </remarks>
    public bool Guard { get; set; } = DEFAULT_IMPOSSIBLE_TRAVEL_GUARD;
    /// <summary>The speed (km/h) used to compare the travel speed between two login attempts. Default is 80 km/h.</summary>
    public double AcceptableSpeed { get; set; } = 80d;
    /// <summary>Specifies the flow to follow when impossible travel is detected for the current user. Defaults to <see cref="ImpossibleTravelFlowType.PromptMfa"/>.</summary>
    public ImpossibleTravelFlowType FlowType { get; set; } = ImpossibleTravelFlowType.PromptMfa;
    /// <summary>Determines whether password related events are persisted in the store.</summary>
    public bool RecordPasswordEvents { get; set; } = false;
    /// <summary>Determines whether token related events are persisted in the store.</summary>
    public bool RecordTokenEvents { get; set; } = false;
}
