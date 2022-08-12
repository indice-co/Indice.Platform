using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Interfaces;

namespace Indice.Features.Cases.Events
{
    /// <summary>
    /// The event that will be raised after a case's final submission.
    /// </summary>
    public class CaseSubmittedEvent : ICaseEvent
    {
        /// <summary>
        /// The case that has been submitted.
        /// </summary>
        public DbCase Case { get; }

        /// <summary>
        /// The case type code that has been submitted.
        /// </summary>
        public string CaseTypeCode { get; set; }

        public CaseSubmittedEvent(DbCase @case, string caseTypeCode) {
            Case = @case;
            CaseTypeCode = caseTypeCode;
        }
    }
}