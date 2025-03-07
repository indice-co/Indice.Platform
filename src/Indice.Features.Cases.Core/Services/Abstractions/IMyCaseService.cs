using System.Security.Claims;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Types;

namespace Indice.Features.Cases.Core.Services.Abstractions;

/// <summary>The services regarding Customer's perspective on case management.</summary>
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
    Task<CreateCaseResponse> CreateDraft(ClaimsPrincipal user, string caseTypeCode, string? groupId, ContactMeta? customer, Dictionary<string, string> metadata, string? channel);

    /// <summary>Update the case with the case data and does a json instance-schema validation of the case type's schema (<see cref="CaseType.DataSchema"/>).</summary>
    /// <param name="user">The user that will update the case.</param>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="data">The case data (as defined by JSON Schema in CaseType).</param>
    Task UpdateData(ClaimsPrincipal user, Guid caseId, dynamic data);

    /// <summary>Submit the case. Case must be in <strong>Case.Draft</strong> mode.</summary>
    /// <param name="user">The user that initiated the submission.</param>
    /// <param name="caseId">The Id of the case.</param>
    Task Submit(ClaimsPrincipal user, Guid caseId);

    /// <summary>Get <see cref="Case"/> for a user by its Id.</summary>
    /// <param name="user">The user that creates the request.</param>
    /// <param name="caseId">The Id of the case.</param>
    Task<Case?> GetCaseById(ClaimsPrincipal user, Guid caseId);

    /// <summary>Get the cases of the User.</summary>
    /// <param name="user">The Id of the user to retrieve the cases.</param>
    /// <param name="options">The user list options.</param>
    Task<ResultSet<MyCasePartial>> GetCases(ClaimsPrincipal user, ListOptions<GetMyCasesListFilter> options);

    /// <summary>Gets a case type</summary>
    /// <param name="caseTypeCode">The case type code</param>
    Task<CaseTypePartial> GetCaseType(string caseTypeCode);

    /// <summary>Get case types list.</summary>
    /// <param name="options">The <see cref="GetMyCaseTypesListFilter"/> options.</param>
    Task<ResultSet<CaseTypePartial>> GetCaseTypes(ListOptions<GetMyCaseTypesListFilter> options);
}
