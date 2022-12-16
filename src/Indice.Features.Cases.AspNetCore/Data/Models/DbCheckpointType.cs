using System;
using System.Linq;

namespace Indice.Features.Cases.Data.Models
{
    public class DbCheckpointType
    {
        public Guid Id { get; set; }
        public Guid CaseTypeId { get; set; }
        public string Code { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Translations { get; set; }
        public CaseStatus Status { get; set; }
        public bool Private { get; set; }
        public virtual DbCaseType CaseType { get; set; }
    }
}