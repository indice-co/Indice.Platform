using System.Security.Claims;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Requests;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;

namespace Indice.Features.Cases.Interfaces;

/// <summary>The Case Type services for managing <see cref="DbCaseType"/> domain model.</summary>
public interface IAccessRuleService
{
    /// <summary>Get the case type a user is authorized for.</summary>
    Task<ResultSet<AccessRule>> Get(ListOptions<GetAccessRulesListFilter> filters);

    Task AdminCreate(ClaimsPrincipal user, AddAccessRuleRequest accessRule);
    Task AdminBatch(ClaimsPrincipal user, List<AddAccessRuleRequest> accessRules);

    Task Create(ClaimsPrincipal user, Guid caseId, AddCaseAccessRuleRequest accessRule);

    Task Batch(ClaimsPrincipal user, Guid caseId, List<AddCaseAccessRuleRequest> accessRules);
    Task<AccessRule> Update(ClaimsPrincipal user, Guid accessRuleId, int accessLevel);


    Task Delete(ClaimsPrincipal user, Guid id);
}