namespace Indice.Features.Cases.Data.Models
{
    public class DbCheckpoint
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CaseId { get; set; }
        public Guid CheckpointTypeId { get; set; }
        public AuditMeta CreatedBy { get; set; }
        public DateTimeOffset? CompletedDate { get; set; }
        public DateTimeOffset? DueDate { get; set; }
        public virtual DbCheckpointType CheckpointType { get; set; }
        public virtual DbCase Case { get; set; }
    }
}