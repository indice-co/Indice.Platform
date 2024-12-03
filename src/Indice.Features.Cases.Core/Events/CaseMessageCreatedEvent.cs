using Indice.Features.Cases.Core.Events.Abstractions;
using Indice.Features.Cases.Core.Models;

namespace Indice.Features.Cases.Core.Events;

/// <summary>The event that will be raised <b>before</b> the case service handles a <see cref="Message"/>.</summary>
public class CaseMessageCreatedEvent : ICaseEvent
{
    /// <summary>The Id of the case.</summary>
    public Guid CaseId { get; set; }
    
    /// <summary>The <see cref="Message"/> that will be sent to the case service.</summary>
    public Message Message { get; set; }

    /// <summary>Construct a new <see cref="CaseMessageCreatedEvent"/>.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="message">The <see cref="Message"/> that will be sent to the case service.</param>
    public CaseMessageCreatedEvent(Guid caseId, Message message) {
        CaseId = caseId;
        Message = message;
    }
}