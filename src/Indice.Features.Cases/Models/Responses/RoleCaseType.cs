using System;

namespace Indice.Features.Cases.Models.Responses
{
    internal class RoleCaseType
    {
        public Guid Id { get; set; }
        public string? RoleName { get; set; }
        public Guid CaseTypeId { get; set; }
        public Guid CheckpointTypeId { get; set; }
        public CaseType CaseType { get; set; }
        public CheckpointType CheckpointType { get; set; }
    }
}
