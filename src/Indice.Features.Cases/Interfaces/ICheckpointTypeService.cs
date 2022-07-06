using System;
using System.Threading.Tasks;
using Indice.Features.Cases.Data.Models;

namespace Indice.Features.Cases.Interfaces
{
    internal interface ICheckpointTypeService
    {
        Task Create(Guid caseTypeId, string name, string? description, CasePublicStatus publicStatus);
        Task<DbCheckpointType> GetCheckpointType(Guid caseTypeId, CasePublicStatus publicStatus);
    }
}