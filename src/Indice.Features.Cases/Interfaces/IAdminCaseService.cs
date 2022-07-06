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
    public interface IAdminCaseService
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
        /// <returns></returns>
        Task<Guid> CreateDraft(ClaimsPrincipal user, string caseTypeCode, string groupId, CustomerMeta customer, Dictionary<string, string> metadata);
        
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
        /// Get a list of cases as defined by <see cref="GetCasesListFilter"/> and the role of the user.
        /// </summary>
        /// <param name="user">The user that initiated the request</param>
        /// <param name="options">The case list filters.</param>
        /// <returns></returns>
        Task<ResultSet<CasePartial>> GetCases(ClaimsPrincipal user, ListOptions<GetCasesListFilter> options);

        /// <summary>
        /// Get a case for a user by its Id
        /// </summary>
        /// <param name="user">The user that creates the request.</param>
        /// <param name="caseId">The Id of the case.</param>
        /// <param name="includeAttachmentData">Include the attachment data with the response.</param>
        /// <returns></returns>
        Task<CaseDetails> GetCaseById(ClaimsPrincipal user, Guid caseId, bool? includeAttachmentData = null);

        /// <summary>
        /// Get an attachment for a user by its Id
        /// </summary>
        /// <param name="user">The user that creates the request.</param>
        /// <param name="attachmentId">The Id of the attachment.</param>
        /// <returns></returns>
        Task<DbAttachment> GetDbAttachmentById(ClaimsPrincipal user, Guid attachmentId);

        /// <summary>
        /// Assign a case to the actor that initiated this method.
        /// </summary>
        /// <param name="user">The user that initiated the call, and will be self-assigned to the case.</param>
        /// <param name="caseId">The Id of the case to be assigned.</param>
        /// <returns></returns>
        Task<AuditMeta> AssignCase(ClaimsPrincipal user, Guid caseId);

        /// <summary>
        /// Clears the assignment for a case.
        /// </summary>
        /// <param name="caseId">The Id of the case.</param>
        /// <returns></returns>
        Task RemoveAssignment(Guid caseId);

        /// <summary>
        /// Get the timeline entries for a case.
        /// </summary>
        /// <param name="user">The user that creates the request.</param>
        /// <param name="caseId">The Id of the case.</param>
        /// <returns></returns>
        Task<IEnumerable<TimelineEntry>> GetTimeline(ClaimsPrincipal user, Guid caseId);

        /// <summary>
        /// Get users that have opted in for notifications for their branch
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        Task<IEnumerable<DbCaseTypeNotificationSubscription>> GetCaseTypeUsersByGroupId(string groupId);

        /// <summary>
        /// Create a new notification subscription for a user and a groupId.
        /// <remarks>If a subscription already exists, this service will force delete the previous subscription.</remarks>
        /// </summary>
        /// <param name="user">The user to create the subscription</param>
        /// <returns></returns>
        Task CreateCaseTypeNotificationSubscription(ClaimsPrincipal user);
    }
}
