using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Models.Responses;

namespace Indice.Features.Cases.Interfaces
{
    internal interface ICaseTypeService
    {
        Task<DbCaseType> Get(string code);
        Task<DbCaseType> Get(Guid id);
        Task<List<CaseType>> Get(ClaimsPrincipal user);
    }
}