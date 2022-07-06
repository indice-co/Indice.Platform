namespace Indice.Features.Cases.Models
{
    public class ApprovalRequest 
    {
        // todo Message ??

        /// <summary>
        /// User action for approval.
        /// </summary>
        public Approval Action { get; set; }

        /// <summary>
        /// User comment related to the action.
        /// </summary>
        public string? Comment { get; set; }
    }
}