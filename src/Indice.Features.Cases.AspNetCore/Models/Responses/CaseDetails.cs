using System.Collections.Generic;

namespace Indice.Features.Cases.Models.Responses
{
    /// <summary>
    /// Models case details.
    /// </summary>
    public class CaseDetails : CasePartial
    {
        /// <summary>
        /// The attachments of the case.
        /// </summary>
        public List<CaseAttachment> Attachments { get; set; } = new List<CaseAttachment>();
    }
}
