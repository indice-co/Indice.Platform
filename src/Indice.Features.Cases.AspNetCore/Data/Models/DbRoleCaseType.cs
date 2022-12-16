using System;

namespace Indice.Features.Cases.Data.Models
{
    public class DbRoleCaseType
    {
        public Guid Id { get; set; }
        public string RoleName { get; set; }
        public Guid CaseTypeId { get; set; }
        public Guid CheckpointTypeId { get; set; }
        public virtual DbCaseType CaseType { get; set; }
        public virtual DbCheckpointType CheckpointType { get; set; }
    }
}