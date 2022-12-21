namespace Indice.Features.Cases.Data.Models
{
#pragma warning disable 1591
    public class DbRoleCaseType
    {
        public Guid Id { get; set; }
        public string RoleName { get; set; }
        public Guid CaseTypeId { get; set; }
        public Guid CheckpointTypeId { get; set; }
        public virtual DbCaseType CaseType { get; set; }
        public virtual DbCheckpointType CheckpointType { get; set; }
    }
#pragma warning restore 1591
}