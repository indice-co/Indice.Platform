using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Core.Services;

internal class NotificationSubscriptionService : INotificationSubscriptionService
{
    private readonly CasesDbContext _dbContext;

    public NotificationSubscriptionService(CasesDbContext dbContext) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<List<NotificationSubscription>> GetSubscriptions(ListOptions<NotificationFilter> options) {
        var filter = options.Filter ?? new NotificationFilter();
        var subscriptions = await _dbContext.NotificationSubscriptions
            .AsQueryable()
            .Where(x => (filter.Email.Length == 0 || filter.Email.Contains(x.Email)) &&
                        (filter.GroupId.Length == 0 || filter.GroupId.Contains(x.GroupId)) &&
                        (filter.CaseTypeIds.Length == 0 || filter.CaseTypeIds.Contains(x.CaseTypeId)))
            .Select(x => new NotificationSubscription {
                CaseTypeId = x.CaseTypeId,
                Email = x.Email,
                GroupId = x.GroupId
            })
            .ToListAsync();
        return subscriptions;
    }

    public async Task Subscribe(List<Guid> caseTypeIds, NotificationSubscription subscriber) {
        if (string.IsNullOrEmpty(subscriber.GroupId)) throw new ArgumentException($"No Group found for subscriber: \"{subscriber.Email}\".");
        if (string.IsNullOrEmpty(subscriber.Email)) throw new ArgumentNullException(nameof(subscriber.Email));
        // remove existing subscriptions
        var entitiesToRemove = await _dbContext.NotificationSubscriptions
            .AsQueryable()
            .Where(u => u.Email == subscriber.Email)
            .ToListAsync();
        if (entitiesToRemove.Any()) {
            _dbContext.RemoveRange(entitiesToRemove);
        }
        // add new subscriptions
        var entitiesToAdd = caseTypeIds.Select(id => new DbNotificationSubscription {
            CaseTypeId = id,
            GroupId = subscriber.GroupId,
            Email = subscriber.Email
        });

        if (entitiesToAdd.Any()) {
            await _dbContext.AddRangeAsync(entitiesToAdd);
        }
        await _dbContext.SaveChangesAsync();
    }

}