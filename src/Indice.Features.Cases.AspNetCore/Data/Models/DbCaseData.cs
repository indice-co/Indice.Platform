namespace Indice.Features.Cases.Data.Models
{
    public class DbCaseData
    {
        public Guid Id { get; set; }
        public Guid CaseId { get; set; }
        public AuditMeta CreatedBy { get; set; }
        public string Data { get; set; }
        public virtual DbCase Case { get; set; }
    }
}