using System;
using System.Linq;

namespace Indice.Features.Cases.Data.Models
{
    public class DbCheckpointType
    {
        public Guid Id { get; set; }
        public Guid CaseTypeId { get; set; }
        public string Code { get; private set; }
        public string? Description { get; set; }
        public CasePublicStatus PublicStatus { get; set; }
        public bool Private{ get; set; }
        public virtual DbCaseType CaseType { get; set; }

        public string CaseTypeCode => Code.Split(":").First();
        public string Name => Code.Split(":").Last();

        public void SetCode(string caseTypeCode, string name) {
            Code = $"{caseTypeCode}:{name}";
        }
    }
}