using System;
using Indice.Features.Cases.Interfaces;

namespace Indice.Features.Cases.Events
{
    /// <summary>
    /// The event that will be raised after the case service wants t
    /// </summary>
    public class CaseDownloadedEvent : ICaseEvent
    {
        /// <summary>
        /// The Id of the case.
        /// </summary>
        public Guid CaseId { get; set; }

        /// <summary>
        /// The code of the case type.
        /// </summary>
        public string CaseTypeCode { get; set; }

        /// <summary>
        /// Construct a new <see cref="CaseDownloadedEvent"/>.
        /// </summary>
        /// <param name="caseId">The Id of the case.</param>
        /// <param name="caseTypeCode">The code of the case type.</param>
        public CaseDownloadedEvent(Guid caseId, string caseTypeCode) {
            CaseId = caseId;
            CaseTypeCode = caseTypeCode;
        }
    }
}
