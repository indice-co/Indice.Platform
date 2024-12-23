﻿using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Security.Claims;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Exceptions;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Requests;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Security;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Core.Services;

internal class AccessRuleService : IAccessRuleService
{
    private readonly CasesDbContext _dbContext;
    private readonly CasesOptions _options;

    public AccessRuleService(CasesDbContext dbContext, IOptions<CasesOptions> options) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _options = options.Value;
    }


    public Task<ResultSet<AccessRule>> Get(ListOptions<GetAccessRulesListFilter> filters) {
        var query = _dbContext.CaseAccessRules
          .AsNoTracking();
        // also: filter CheckpointTypeIds
        if (filters.Filter.Checkpoint.HasValue && Guid.TryParse(filters.Filter.Checkpoint.Value.Value, out var checkpointTypeId)) {
            query = filters.Filter.Checkpoint.Value.Operator switch {
                FilterOperator.Eq => query.Where(c => c.RuleCheckpointTypeId == checkpointTypeId),
                FilterOperator.Neq => query.Where(c => c.RuleCheckpointTypeId != checkpointTypeId),
                _ => query
            };
        }

        if (filters.Filter.CaseType.HasValue) {
            query = (filters.Filter.CaseType.Value.Operator, Guid.TryParse(filters.Filter.CaseType.Value.Value, out var caseTypeId)) switch {
                (FilterOperator.Eq, true) => query.Where(c => c.RuleCaseId == caseTypeId),
                (FilterOperator.Neq, true) => query.Where(c => c.RuleCaseId != caseTypeId),
                _ => query
            };
        }

        if (filters.Filter.GroupId.HasValue) {
            query = (filters.Filter.GroupId.Value.Operator, filters.Filter.GroupId.Value.Value) switch {
                (FilterOperator.Eq, string groupId) => query.Where(c => c.MemberGroupId == groupId),
                (FilterOperator.Neq, string groupId) => query.Where(c => c.MemberGroupId != groupId),
                _ => query
            };
        }

        if (filters.Filter.Role.HasValue) {
            query = (filters.Filter.Role.Value.Operator, filters.Filter.Role.Value.Value) switch {
                (FilterOperator.Eq, string role) => query.Where(c => c.MemberRole == role),
                (FilterOperator.Neq, string role) => query.Where(c => c.MemberRole != role),
                _ => query
            };
        }

        return query.Select(ToModelExpression())
        .ToResultSetAsync(filters);
    }

    public async Task<List<AccessRule>> GetCaseAccessRules(Guid caseId) {
        var @case = await _dbContext.Cases
        .AsNoTracking()
        .FirstAsync(x => x.Id == caseId);

        var checkpoints = _dbContext.CheckpointTypes
            .AsNoTracking()
            .Where(x => x.CaseTypeId == @case.CaseTypeId)
            .Select(x => x.Id);

        var query = _dbContext.CaseAccessRules
            .AsNoTracking()
            .Where(x =>
                x.RuleCaseId == caseId ||
                x.RuleCaseTypeId == @case.CaseTypeId ||
                (x.RuleCaseId == null && checkpoints.Contains(x.RuleCheckpointTypeId ?? Guid.Empty))
                );

        return await query.Select(ToModelExpression()).ToListAsync();
    }

    public async Task AdminCreate(ClaimsPrincipal user, AddAccessRuleRequest accessRule) {
        // if client is systemic or admin, then bypass checks since no filtering is required.
        var isSystemOrAdmin = user.IsSystemClient() || user.IsAdmin();
        if (!isSystemOrAdmin) {
            throw new UnauthorizedAccessException("User does not have administrator rights.");
        }
        if (!accessRule.IsValid())
            throw new ValidationException("At least one resource rule must be set (RuleCaseId, RuleCheckpointTypeId, RuleCaseTypeId or RuleCaseId & RuleCheckpointTypeId) with at least one grant (MemberRole, MemberGroupId, MemberUserId).");

        var entity = FromModel(accessRule);
        await _dbContext.CaseAccessRules.AddAsync(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task AdminBatch(ClaimsPrincipal user, List<AddAccessRuleRequest> accessRules) {
        // if client is systemic or admin, then bypass checks since no filtering is required.
        var isSystemOrAdmin = ((user.HasClaim(BasicClaimTypes.Scope, _options.RequiredScope) && user.IsSystemClient()) || user.IsAdmin());

        if (!isSystemOrAdmin) {
            throw new UnauthorizedAccessException("User does not have administrator rights.");
        }
        if (accessRules.Exists(x => !x.IsValid()))
            throw new ValidationException("At least one resource rule must be set (RuleCaseId, RuleCheckpointTypeId, RuleCaseTypeId or RuleCaseId & RuleCheckpointTypeId) with at least one grant (MemberRole, MemberGroupId, MemberUserId) for all records.");

        foreach (var accessRule in accessRules) {
            await _dbContext.CaseAccessRules.AddAsync(FromModel(accessRule));
        }

        await _dbContext.SaveChangesAsync();
    }


    public async Task Create(ClaimsPrincipal user, Guid caseId, AddCaseAccessRuleRequest accessRule) {

        if (!accessRule.IsValid())
            throw new ValidationException("At lease on of the following fields must be set MemberRole, MemberGroupId, MemberUserId.");

        var entity = FromModel(accessRule, caseId);
        await _dbContext.CaseAccessRules.AddAsync(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<AccessRule> Update(ClaimsPrincipal user, Guid accessRuleId, int accessLevel) {

        // if client is systemic or admin, then bypass checks since no filtering is required.
        var isSystemOrAdmin = ((user.HasClaim(BasicClaimTypes.Scope, CasesCoreConstants.DefaultScopeName) && user.IsSystemClient()) || user.IsAdmin());

        var dbAccessRule = await _dbContext.CaseAccessRules
                             .AsQueryable()
                             .FirstOrDefaultAsync(x => x.Id == accessRuleId) ?? throw new AccessRuleFoundException("Rule was not not found.");

        if (!isSystemOrAdmin && dbAccessRule.RuleCaseId is null) {
            throw new UnauthorizedAccessException("Only admin users can update this rule");
        }
        // Update case type entity
        dbAccessRule.AccessLevel = accessLevel;
        _dbContext.CaseAccessRules.Update(dbAccessRule);
        await _dbContext.SaveChangesAsync();
        return ToModel(dbAccessRule);
    }

    public async Task Batch(ClaimsPrincipal user, Guid caseId, List<AddCaseAccessRuleRequest> accessRules) {

        if (accessRules.Exists(x => !x.IsValid()))
            throw new ValidationException("At lease on of the following fields must be set MemberRole, MemberGroupId, MemberUserId for all records.");

        foreach (var accessRule in accessRules) {
            await _dbContext.CaseAccessRules.AddAsync(FromModel(accessRule, caseId));
        }
        await _dbContext.SaveChangesAsync();
    }

    public async Task Delete(ClaimsPrincipal user, Guid id) {
        // if client is systemic or admin, then bypass checks since no filtering is required.
        var isSystemOrAdmin = ((user.HasClaim(BasicClaimTypes.Scope, _options.RequiredScope) && user.IsSystemClient()) || user.IsAdmin());

        var dbAccessRule = await _dbContext.CaseAccessRules
                             .AsQueryable()
                             .FirstOrDefaultAsync(x => x.Id == id) ?? throw new AccessRuleFoundException("Rule was not not found.");

        if (!isSystemOrAdmin && dbAccessRule.RuleCaseId is null) {
            throw new UnauthorizedAccessException("Only admin users can update this rule");
        }
        _dbContext.CaseAccessRules.Remove(dbAccessRule);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> ReplaceUser(ClaimsPrincipal user, Guid caseId, string existingUserId, string newUserId) {
        var query = _dbContext.CaseAccessRules
            .Where(x =>
                x.RuleCaseId == caseId &&
                x.MemberUserId == existingUserId);

        foreach (var existingAccessRule in query) {
            existingAccessRule.MemberUserId = newUserId;
        }
        var updated = await _dbContext.SaveChangesAsync();
        return updated > 0;
    }

    private DbCaseAccessRule FromModel(AddAccessRuleRequest accessRule) =>
        new () {
            Id = Guid.NewGuid(),

            RuleCaseId = accessRule.RuleCaseId,
            RuleCheckpointTypeId = accessRule.RuleCheckpointTypeId,
            RuleCaseTypeId = accessRule.RuleCaseTypeId,

            MemberRole = accessRule.MemberRole,
            MemberGroupId = accessRule.MemberGroupId,
            MemberUserId = accessRule.MemberUserId,

            AccessLevel = accessRule.AccessLevel,
            CreatedDate = DateTimeOffset.UtcNow
        };
    private DbCaseAccessRule FromModel(AddCaseAccessRuleRequest accessRule, Guid caseId) =>
       new () {
           Id = Guid.NewGuid(),

           RuleCaseId = caseId,
           RuleCheckpointTypeId = accessRule.RuleCheckpointTypeId,

           MemberRole = accessRule.MemberRole,
           MemberGroupId = accessRule.MemberGroupId,
           MemberUserId = accessRule.MemberUserId,

           AccessLevel = accessRule.AccessLevel,
           CreatedDate = DateTimeOffset.UtcNow
       };

    private AccessRule ToModel(DbCaseAccessRule accessRule) => ToModelExpression().Compile(false)(accessRule);
    private Expression<Func<DbCaseAccessRule, AccessRule>> ToModelExpression() => (DbCaseAccessRule accessRule) => new AccessRule {
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