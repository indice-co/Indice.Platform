using System.Linq.Expressions;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Exceptions;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Core.Services;

internal class AdminDashboardService : IAdminDashboardService
{
    /// <summary>Intake is always queried for at least two weeks so the week-over-week figures can be derived from it.</summary>
    private const int MinIntakeWindowDays = 14;
    private readonly CasesDbContext _dbContext;
    private readonly ICaseAuthorizationProvider _memberAuthorizationProvider;
    private readonly CasesOptions _options;

    public AdminDashboardService(
        CasesDbContext dbContext,
        ICaseAuthorizationProvider memberAuthorizationProvider,
        IOptions<CasesOptions> options) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _memberAuthorizationProvider = memberAuthorizationProvider ?? throw new ArgumentNullException(nameof(memberAuthorizationProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<CaseDashboardMetrics> GetMetrics(UserActor user, int intakeDays, int staleAfterDays) {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentOutOfRangeException.ThrowIfLessThan(intakeDays, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(staleAfterDays, 1);

        var now = DateTimeOffset.Now;
        var today = DateOnly.FromDateTime(now.Date);
        var metrics = new CaseDashboardMetrics {
            StaleAfterDays = staleAfterDays,
            GeneratedAt = now
        };

        var visible = await GetVisibleCases(user);
        if (visible is null) {
            metrics.Intake = EmptyIntake(today, intakeDays);
            return metrics;
        }

        var statusCounts = await visible
            .GroupBy(x => x.CheckpointType.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync();
        int CountOf(CaseStatus status) => statusCounts.FirstOrDefault(x => x.Status == status)?.Count ?? 0;
        metrics.Statuses = new CaseStatusCounts {
            Submitted = CountOf(CaseStatus.Submitted),
            InProgress = CountOf(CaseStatus.InProgress),
            Completed = CountOf(CaseStatus.Completed),
            Deleted = CountOf(CaseStatus.Deleted)
        };

        var userId = user.Id;
        metrics.AssignedToMe = await CountOpen(await GetVisibleCases(user, c => c.AssignedTo!.Id == userId));
        metrics.Unassigned = await CountOpen(await GetVisibleCases(user, c => c.AssignedTo!.Id == null));
        // stale = created on or before (today - staleAfterDays), i.e. the same range the case list's "to" date filter selects
        var staleBefore = StartOfDay(today.AddDays(-staleAfterDays + 1), now.Offset);
        metrics.Stale = await CountOpen(visible.Where(x => x.CreatedByWhen < staleBefore));

        var window = Math.Max(intakeDays, MinIntakeWindowDays);
        var windowStart = today.AddDays(-window + 1);
        var windowStartOffset = StartOfDay(windowStart, now.Offset);
        var perDay = await visible
            .Where(x => x.CreatedByWhen >= windowStartOffset)
            .GroupBy(x => x.CreatedByWhen!.Value.Date)
            .Select(group => new { Date = group.Key, Count = group.Count() })
            .ToListAsync();
        var countsByDay = perDay.ToDictionary(x => DateOnly.FromDateTime(x.Date), x => x.Count);
        int CountOn(DateOnly day) => countsByDay.TryGetValue(day, out var count) ? count : 0;

        metrics.Intake = Enumerable.Range(0, intakeDays)
            .Select(offset => today.AddDays(offset - intakeDays + 1))
            .Select(day => new DailyCaseCount { Date = day, Count = CountOn(day) })
            .ToList();
        metrics.NewThisWeek = Enumerable.Range(0, 7).Sum(offset => CountOn(today.AddDays(-offset)));
        metrics.NewPreviousWeek = Enumerable.Range(7, 7).Sum(offset => CountOn(today.AddDays(-offset)));
        return metrics;
    }

    /// <summary>
    /// The non-draft cases the user can see, with the same visibility rules as <see cref="AdminCaseService.GetCases"/>: elevated users bypass
    /// access rules when <see cref="CasesOptions.ByPassAccessRulesForElevatedUsers"/> is set, everyone else needs a matching access rule, and the
    /// <see cref="ICaseAuthorizationProvider"/> has the final say. Returns <see langword="null"/> when the user may not list cases at all.
    /// </summary>
    private async Task<IQueryable<CasePartial>?> GetVisibleCases(UserActor user, Expression<Func<DbCase, bool>>? filter = null) {
        var cases = _dbContext.Cases
            .AsNoTracking()
            .Where(c => !c.Draft);
        if (filter is not null) {
            cases = cases.Where(filter);
        }

        var ignoreAccessRules = (user.IsSystemClient || user.IsAdmin) && _options.ByPassAccessRulesForElevatedUsers;
        if (!ignoreAccessRules) {
            var accessMatch = DbCaseAccessRule.AccessMatchPredicate(user.Id, user.Roles, user.GroupId);
            cases = cases.Where(c => c.CheckpointId != null && _dbContext.CaseAccessRules
                .Where(accessMatch)
                .Any(x =>
                    (x.RuleCaseId == c.Id && x.RuleCaseTypeId == null && x.RuleCheckpointTypeId == null) ||
                    (x.RuleCaseId == null && x.RuleCaseTypeId == c.CaseTypeId && x.RuleCheckpointTypeId == null) ||
                    (x.RuleCaseId == null && x.RuleCaseTypeId == null && x.RuleCheckpointTypeId == c.Checkpoint.CheckpointTypeId) ||
                    (x.RuleCaseId == c.Id && x.RuleCaseTypeId == null && x.RuleCheckpointTypeId == c.Checkpoint.CheckpointTypeId)));
        }

        var query = cases.Select(@case => new CasePartial {
            Id = @case.Id,
            OwnerId = @case.Owner.Reference,
            CreatedByWhen = @case.CreatedBy.When,
            CaseType = new CaseTypePartial {
                Id = @case.CaseType.Id,
                Code = @case.CaseType.Code
            },
            Channel = @case.Channel,
            GroupId = @case.GroupId,
            CheckpointType = new CheckpointType {
                Id = @case.Checkpoint.CheckpointType.Id,
                Code = @case.Checkpoint.CheckpointType.Code,
                Status = @case.Checkpoint.CheckpointType.Status
            }
        });

        // same contract as the case list: an authorization provider may refuse the listing altogether
        try {
            return await _memberAuthorizationProvider.GetCaseMembership(query, user);
        } catch (ResourceUnauthorizedException) {
            return null;
        }
    }

    private static async Task<int> CountOpen(IQueryable<CasePartial>? cases) => cases is null
        ? 0
        : await cases.CountAsync(x => x.CheckpointType.Status == CaseStatus.Submitted || x.CheckpointType.Status == CaseStatus.InProgress);

    private static DateTimeOffset StartOfDay(DateOnly day, TimeSpan offset) => new(day.ToDateTime(TimeOnly.MinValue), offset);

    private static List<DailyCaseCount> EmptyIntake(DateOnly today, int days) => Enumerable.Range(0, days)
        .Select(offset => new DailyCaseCount { Date = today.AddDays(offset - days + 1), Count = 0 })
        .ToList();
}
