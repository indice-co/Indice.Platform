using System.Collections.Generic;

namespace Indice.Features.Cases.Models.Responses
{
    /// <summary>
    /// Models case details.
    /// </summary>
    public class CaseDetails : CasePartial
    {
        public List<CaseAttachment> Attachments { get; set; } = new List<CaseAttachment>();
    }
}
