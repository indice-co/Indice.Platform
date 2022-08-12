using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;

namespace Indice.Features.Cases.Interfaces
{
    /// <summary>
    /// The Case Type services for managing <see cref="DbCaseType"/> domain model.
    /// </summary>
    internal interface ICaseTypeService
    {
        /// <summary>
        /// Get a case type by its code.
        /// </summary>
        /// <param name="code">The case type code.</param>
        Task<DbCaseType> Get(string code);

        /// <summary>
        /// Get a case type by its Id.
        /// </summary>
        /// <param name="id">The Id of the case type.</param>
        Task<DbCaseType> Get(Guid id);
        
        /// <summary>
        /// Get the case type a user is authorized for.
        /// </summary>
        Task<ResultSet<CaseTypePartial>> Get(ClaimsPrincipal user);
        Task<CaseTypeDetails> GetCaseTypeDetailsById(Guid id);
        Task<CaseTypeDetails> Update(CaseTypeRequest caseType);
        Task Create(CaseTypeRequest caseType);
    }
}