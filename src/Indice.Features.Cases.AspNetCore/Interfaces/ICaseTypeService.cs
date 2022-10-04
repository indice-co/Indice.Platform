using System;
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
        /// <param name="user"></param>
        /// <param name="isForCaseCreation">Differentiates between the case types that an admin user can 1) view and 2) select for a case creation</param>
        Task<ResultSet<CaseTypePartial>> Get(ClaimsPrincipal user, bool isForCaseCreation);

        /// <summary>
        /// Get the case type details by its Id.
        /// </summary>
        /// <param name="id">The case type Id.</param>
        /// <returns></returns>
        Task<CaseTypeDetails> GetCaseTypeDetailsById(Guid id);

        /// <summary>
        /// Create a new case type.
        /// </summary>
        /// <param name="caseType">The case type request.</param>
        /// <returns></returns>
        Task Create(CaseTypeRequest caseType);

        /// <summary>
        /// Update a case type code.
        /// </summary>
        /// <param name="caseType">The case type code request.</param>
        /// <returns></returns>
        Task<CaseTypeDetails> Update(CaseTypeRequest caseType);

        /// <summary>
        /// Delete a case type, if there are no case instances created.
        /// </summary>
        /// <param name="id">The Id of the case type.</param>
        /// <returns></returns>
        Task Delete(Guid id);
    }
}