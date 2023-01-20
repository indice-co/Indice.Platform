namespace Indice.Features.Cases.Data.Models
{
#pragma warning disable 1591
    public class DbCase
    {
        public Guid Id { get; set; }
        public Guid CaseTypeId { get; set; }
        public Priority Priority { get; set; }
        /// <summary>
        /// case belongs to this customer, regardless of whether he created it or not
        /// </summary>
        public CustomerMeta Customer { get; set; }
        public string GroupId { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
        public Guid? PublicCheckpointId { get; set; }
        public Guid? CheckpointId { get; set; }
        public Guid? PublicDataId { get; set; }
        public Guid? DataId { get; set; }
        public AuditMeta CreatedBy { get; set; }
        public AuditMeta CompletedBy { get; set; }
        public AuditMeta AssignedTo { get; set; }
        public bool Draft { get; set; }
        public string Channel { get; set; }
        public virtual DbCaseType CaseType { get; set; }
        public virtual DbCheckpoint PublicCheckpoint { get; set; }
        public virtual DbCheckpoint Checkpoint { get; set; }
        public virtual DbCaseData PublicData { get; set; }
        public virtual DbCaseData Data { get; set; }
        public virtual List<DbAttachment> Attachments { get; set; } = new List<DbAttachment>();
        public virtual List<DbCheckpoint> Checkpoints { get; set; } = new List<DbCheckpoint>();
        public virtual List<DbComment> Comments { get; set; } = new List<DbComment>();
        public virtual List<DbCaseData> Versions { get; set; } = new List<DbCaseData>();
        public virtual List<DbCaseApproval> Approvals { get; set; } = new List<DbCaseApproval>();
    }
#pragma warning restore 1591
}