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
    public interface IMyCaseService
    {
        /// <summary>
        /// Create a new case in draft mode. In draft mode we are creating a default checkpoint "Submitted" where the user is able to add attachments before the
        /// final case submission.
        /// </summary>
        /// <param name="user">The user that initiated the case.</param>
        /// <param name="caseTypeCode">The case type code.</param>
        /// <param name="groupId">The id to group the case (eg in banking business it can be the BranchId of the customer).</param>
        /// <param name="customer">The customer metadata that initiated the case.</param>
        /// <param name="metadata">The metadata the case might have.</param>
        /// <param name="channel">The channel the case was created from.</param>
        /// <returns></returns>
        Task<CreateCaseResponse> CreateDraft(ClaimsPrincipal user, string caseTypeCode, string groupId, CustomerMeta customer, Dictionary<string, string> metadata, string? channel);

        /// <summary>
        /// Update the case with the case data and does a json instance-schema validation of the case type's schema (<see cref="DbCaseType.DataSchema"/>).
        /// </summary>
        /// <param name="user">The user that will update the case.</param>
        /// <param name="caseId">The Id of the case.</param>
        /// <param name="data">The case data (as defined by JSON Schema in CaseType).</param>
        /// <returns></returns>
        Task UpdateData(ClaimsPrincipal user, Guid caseId, string data);

        /// <summary>
        /// Submit the case. Case must be in <see cref="DbCase.Draft"/> mode.
        /// </summary>
        /// <param name="user">The user that initiated the submission.</param>
        /// <param name="caseId">The Id of the case.</param>
        /// <returns></returns>
        Task Submit(ClaimsPrincipal user, Guid caseId);

        /// <summary>
        /// Get a case for a user by its Id
        /// </summary>
        /// <param name="user">The user that creates the request.</param>
        /// <param name="caseId">The Id of the case.</param>
        /// <returns></returns>
        Task<CaseDetails> GetCaseById(ClaimsPrincipal user, Guid caseId);

        Task<MyCasePartial> GetMyCaseById(ClaimsPrincipal user, Guid caseId);

        /// <summary>
        /// Get the cases of the User.
        /// </summary>
        /// <param name="user">The Id of the user to retrieve the cases.</param>
        /// <param name="options">The user list options.</param>
        /// <returns></returns>
        Task<ResultSet<MyCasePartial>> GetCases(ClaimsPrincipal user, ListOptions<GetMyCasesListFilter> options);

        /// <summary>
        /// Gets a case type
        /// </summary>
        /// <param name="caseTypeCode"></param>
        Task<CaseType> GetCaseType(string caseTypeCode);
    }
}
