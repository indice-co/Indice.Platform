namespace Indice.Features.Messages.Core;

/// <summary>Options used to configure the Message analytics feature.</summary>
public class AnalyticsOptions
{
    /// <summary>Feature flag that can switch on/off the analytics feature. </summary>
    /// <remarks>If turned off the system stops tracking message events. Defaults to <c>true</c>.</remarks>
    public bool Enabled { get; set; } = true;
}
