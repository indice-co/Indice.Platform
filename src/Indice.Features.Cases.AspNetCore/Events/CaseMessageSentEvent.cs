using System;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;

namespace Indice.Features.Cases.Events
{
    /// <summary>
    /// The event that will be raised after the case service handles a <see cref="Message"/>.
    /// </summary>
    public class CaseMessageSentEvent : ICaseEvent
    {
        /// <summary>
        /// The Id of the case.
        /// </summary>
        public Guid CaseId { get; set; }

        /// <summary>
        /// The <see cref="Message"/> that has been sent to the case service.
        /// </summary>
        public Message Message { get; set; }

        public CaseMessageSentEvent(Guid caseId, Message message) {
            CaseId = caseId;
            Message = message;
        }
    }
}