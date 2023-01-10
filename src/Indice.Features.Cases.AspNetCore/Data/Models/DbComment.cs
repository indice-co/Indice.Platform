namespace Indice.Features.Cases.Data.Models
{
#pragma warning disable 1591
    public class DbComment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CaseId { get; set; }
        public Guid? ReplyToCommentId { get; set; }
        public Guid? AttachmentId { get; set; }
        public string Text { get; set; }
        public AuditMeta CreatedBy { get; set; }
        public bool IsCustomer { get; set; }
        public bool Private { get; set; }
        public virtual DbCase Case { get; set; }
        public virtual DbAttachment Attachment { get; set; }
    }
#pragma warning restore 1591
}