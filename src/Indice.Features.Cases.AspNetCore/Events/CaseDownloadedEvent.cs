using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models.Responses;

namespace Indice.Features.Cases.Events;

/// <summary>The event that will be raised when the case pdf is downloaded</summary>
public class CaseDownloadedEvent : ICaseEvent
{
    /// <summary>The case.</summary>
    public Case Case { get; set; }
    /// <summary>The channel from where the event was triggered.</summary>
    public string Channel { get; set; }

    /// <summary>Construct a new <see cref="CaseDownloadedEvent"/>.</summary>
    /// <param name="case">The case.</param>
    /// <param name="channel">The channel from where the event was triggered.</param>
    public CaseDownloadedEvent(Case @case, string channel) {
        Case = @case;
        Channel = channel;
    }
}
