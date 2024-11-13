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
    /// <summary>
    /// Returns a list fo the rule based on the filters
    /// </summary>
    /// <param name="filters">Query filters</param>
    /// <returns></returns>
    Task<ResultSet<AccessRule>> Get(ListOptions<GetAccessRulesListFilter> filters);
    /// <summary>
    /// Return list of access rules for the case specified
    /// </summary>
    /// <param name="caseId">Case id</param>
    /// <returns>List of access rules to be added</returns>
    Task<List<AccessRule>> GetCaseAccessRules(Guid caseId);
    /// <summary>
    /// Allows Admin users to add a new access rule
    /// </summary>
    /// <param name="user">The user that will create the access rule.</param>
    /// <param name="accessRule">The access rule to be added</param>
    /// <returns></returns>
    Task AdminCreate(ClaimsPrincipal user, AddAccessRuleRequest accessRule);
    /// <summary>
    /// Add a list case rule for admin users. This allows user to add more generic rules
    /// </summary>
    /// <param name="user">The user that will create the access rule.</param>
    /// <param name="accessRules">List of accessrules</param>
    /// <returns></returns>
    Task AdminBatch(ClaimsPrincipal user, List<AddAccessRuleRequest> accessRules);
    /// <summary>
    /// Add case rule for admin users. This allows user to add more generic rules
    /// </summary>
    /// <param name="user">The user that will update the access rule.</param>
    /// <param name="caseId">The case to which the rule must be added</param>
    /// <param name="accessRule">The access rule to be added</param>
    /// <returns></returns>
    Task Create(ClaimsPrincipal user, Guid caseId, AddCaseAccessRuleRequest accessRule);

    /// <summary>
    /// Add a list case rule for admin users. 
    /// </summary>
    /// <param name="user">The user that will update the access rule.</param>
    /// <param name="caseId">The case to which the rule must be added</param>
    /// <param name="accessRules">The list of access rules to be added</param>
    /// <returns></returns>
    Task Batch(ClaimsPrincipal user, Guid caseId, List<AddCaseAccessRuleRequest> accessRules);
    /// <summary>
    /// Updates access rule'w access level
    /// </summary>
    /// <param name="user">The user that will update the access rule.</param>
    /// <param name="accessRuleId">The id of the rule to be updated</param>
    /// <param name="accessLevel">The new level to which the rule will be updated</param>
    /// <returns></returns>
    Task<AccessRule> Update(ClaimsPrincipal user, Guid accessRuleId, int accessLevel);

    /// <summary>
    /// Delete the specified access rule
    /// </summary>
    /// <param name="user">The user that will update the access rule.</param>
    /// <param name="id">The Id of the rule to be deleted</param>
    /// <returns></returns>
    Task Delete(ClaimsPrincipal user, Guid id);
}