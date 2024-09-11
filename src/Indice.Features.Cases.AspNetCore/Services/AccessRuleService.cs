using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Requests;
using Indice.Security;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Indice.Features.Cases.Models.Responses;
using Indice.Features.Cases.Exceptions;

namespace Indice.Features.Cases.Services;

internal class AccessRuleService : IAccessRuleService
{
    private readonly CasesDbContext _dbContext;

    public AccessRuleService(CasesDbContext dbContext) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }


    public Task<ResultSet<AccessRule>> Get(ListOptions<GetAccessRulesListFilter> filters) {
        return _dbContext.CaseAccessRules
          .AsNoTracking()
          .Where(filters.Filter.Metadata)// filter Metadata
          .Select(rule => new AccessRule {
              Id = rule.Id,
              AccessLevel = rule.AccessLevel,
              RuleCaseId = rule.RuleCaseId,
              RuleCaseTypeId = rule.RuleCaseTypeId,
              RuleCheckpointTypeId = rule.RuleCheckpointTypeId,
              MemberRole = rule.MemberRole,
              MemberGroupId = rule.MemberGroupId,
              MemberUserId = rule.MemberUserId
          })
        .ToResultSetAsync(filters);
    }

    public async Task AdminCreate(ClaimsPrincipal user, AddAccessRuleRequest accessRule) {
        // if client is systemic or admin, then bypass checks since no filtering is required.
        var isSystemOrAdmin = ((user.HasClaim(BasicClaimTypes.Scope, CasesApiConstants.Scope) && user.IsSystemClient()) || user.IsAdmin());

        if (!isSystemOrAdmin) {
            throw new ValidationException("User does not have administrator rights.");
        }
        if (!accessRule.IsValid())
            throw new ValidationException("At least one resource Id must be set (RuleCaseId, RuleCheckpointTypeId, RuleCaseTypeId) with at least one grant (MemberRole, MemberGroupId, MemberUserId).");

        var entity = ToDbObject(accessRule);
        await _dbContext.CaseAccessRules.AddAsync(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task AdminBatch(ClaimsPrincipal user, List<AddAccessRuleRequest> accessRules) {
        // if client is systemic or admin, then bypass checks since no filtering is required.
        var isSystemOrAdmin = ((user.HasClaim(BasicClaimTypes.Scope, CasesApiConstants.Scope) && user.IsSystemClient()) || user.IsAdmin());

        if (!isSystemOrAdmin) {
            throw new ValidationException("User does not have administrator rights.");
        }
        if (accessRules.Exists(x => !x.IsValid()))
            throw new ValidationException("At least one resource Id must be set (RuleCaseId, RuleCheckpointTypeId, RuleCaseTypeId) with a grant (MemberRole, MemberGroupId, MemberUserId)  for all records.");

        foreach (var accessRule in accessRules) {
            await _dbContext.CaseAccessRules.AddAsync(ToDbObject(accessRule));
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task<AccessRule> AdminUpdate(ClaimsPrincipal user, Guid accessRuleId, int accessLevel) {
       

        var dbAccessRule = await _dbContext.CaseAccessRules
                            .AsQueryable()
                            .FirstOrDefaultAsync(x => x.Id == accessRuleId) ?? throw new ValidationException("Case type code cannot be changed.");

        // Update case type entity
        dbAccessRule.AccessLevel = accessLevel;
        _dbContext.CaseAccessRules.Update(dbAccessRule);
        await _dbContext.SaveChangesAsync();
        return ToDto(dbAccessRule);
    }



    public async Task Create(ClaimsPrincipal user, Guid caseId, AddCaseAccessRuleRequest accessRule) {

        if (!accessRule.IsValid())
            throw new ValidationException("At lease on of the following fields must be set MemberRole, MemberGroupId, MemberUserId.");

        var entity = ToDbObject(accessRule, caseId);
        await _dbContext.CaseAccessRules.AddAsync(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<AccessRule> Update(ClaimsPrincipal user, Guid accessRuleId, int accessLevel) {

        // if client is systemic or admin, then bypass checks since no filtering is required.
        var isSystemOrAdmin = ((user.HasClaim(BasicClaimTypes.Scope, CasesApiConstants.Scope) && user.IsSystemClient()) || user.IsAdmin());

        var dbAccessRule = await _dbContext.CaseAccessRules
                             .AsQueryable()
                             .FirstOrDefaultAsync(x => x.Id == accessRuleId) ?? throw new AccessRuleFoundException("Rule was not not found.");

        if (!isSystemOrAdmin && dbAccessRule.RuleCaseId is null) {
            throw new ValidationException("Only admin users can update this rule");
        }
        // Update case type entity
        dbAccessRule.AccessLevel = accessLevel;
        _dbContext.CaseAccessRules.Update(dbAccessRule);
        await _dbContext.SaveChangesAsync();
        return ToDto(dbAccessRule);
    }

    public async Task Batch(ClaimsPrincipal user, Guid caseId, List<AddCaseAccessRuleRequest> accessRules) {

        if (accessRules.Exists(x => !x.IsValid()))
            throw new ValidationException("At lease on of the following fields must be set MemberRole, MemberGroupId, MemberUserId for all records.");

        foreach (var accessRule in accessRules) {
            await _dbContext.CaseAccessRules.AddAsync(ToDbObject(accessRule, caseId));
        }
        await _dbContext.SaveChangesAsync();
    }

    public async Task Delete(ClaimsPrincipal user, Guid id) {
        // if client is systemic or admin, then bypass checks since no filtering is required.
        var isSystemOrAdmin = ((user.HasClaim(BasicClaimTypes.Scope, CasesApiConstants.Scope) && user.IsSystemClient()) || user.IsAdmin());

        var dbAccessRule = await _dbContext.CaseAccessRules
                             .AsQueryable()
                             .FirstOrDefaultAsync(x => x.Id == id) ?? throw new AccessRuleFoundException("Rule was not not found.");

        if (!isSystemOrAdmin && dbAccessRule.RuleCaseId is null) {
            throw new ValidationException("Only admin users can update this rule");
        }
        _dbContext.CaseAccessRules.Remove(dbAccessRule);
        await _dbContext.SaveChangesAsync();
    }

    private DbCaseAccessRule ToDbObject(AddAccessRuleRequest accessRule) =>
        new DbCaseAccessRule {
            Id = Guid.NewGuid(),

            RuleCaseId = accessRule.RuleCaseId,
            RuleCheckpointTypeId = accessRule.RuleCheckpointTypeId,
            RuleCaseTypeId = accessRule.RuleCaseTypeId,

            MemberRole = accessRule.MemberRole,
            MemberGroupId = accessRule.MemberGroupId,
            MemberUserId = accessRule.MemberUserId,

            AccessLevel = accessRule.AccessLevel,
            CreatedDate = DateTimeOffset.Now
        };
    private DbCaseAccessRule ToDbObject(AddCaseAccessRuleRequest accessRule, Guid caseId) =>
       new DbCaseAccessRule {
           Id = Guid.NewGuid(),

           RuleCaseId = caseId,
           RuleCheckpointTypeId = accessRule.RuleCheckpointTypeId,

           MemberRole = accessRule.MemberRole,
           MemberGroupId = accessRule.MemberGroupId,
           MemberUserId = accessRule.MemberUserId,

           AccessLevel = accessRule.AccessLevel,
           CreatedDate = DateTimeOffset.Now
       };
    private AccessRule ToDto(DbCaseAccessRule accessRule) =>
        new AccessRule {
            Id = accessRule.Id,
            RuleCaseId = accessRule.RuleCaseId,
            RuleCheckpointTypeId = accessRule.RuleCheckpointTypeId,
            RuleCaseTypeId = accessRule.RuleCaseTypeId,
            MemberRole = accessRule.MemberRole,
            MemberGroupId = accessRule.MemberGroupId,
            MemberUserId = accessRule.MemberUserId,
            AccessLevel = accessRule.AccessLevel
        };
}