using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Models;
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

    public async Task<ResultSet<NotificationSubscription>> GetSubscribers(ListOptions<NotificationFilter> options) {
        var filter = options.Filter ?? new NotificationFilter();
        var subscriptions = await _dbContext.NotificationSubscriptions
            .AsQueryable()
            .Where(x => (filter.Email.Count == 0 || filter.Email.Contains(x.Subscriber.Email)) &&
                        (filter.GroupId.Count == 0 || (x.Subscriber.GroupId != null && filter.GroupId.Contains(x.Subscriber.GroupId))) &&
                        (filter.CaseTypeIds.Count == 0 || filter.CaseTypeIds.Contains(x.CaseTypeId)))
            .Select(x => new NotificationSubscription {
                CaseTypeId = x.CaseTypeId,
                Subscriber = new Subscriber {
                    Email = x.Subscriber.Email,
                    GroupId = x.Subscriber.GroupId
                }
            })
            .ToResultSetAsync(options);
        return subscriptions;
    }

    public async Task Subscribe(Subscriber subscriber, Guid caseTypeId, params Guid[]? otherCaseTypeIds) {
        if (subscriber is null || subscriber.IsEmpty()) {
            throw new ArgumentException("Subscriber cannot be null or empty.");
        }

        List<Guid> caseTypeIds = [caseTypeId, ..otherCaseTypeIds ?? []];

        // remove existing subscriptions
        var entitiesToRemove = await _dbContext.NotificationSubscriptions
            .AsQueryable()
            .Where(u => u.Subscriber.Email == subscriber.Email)
            .ToListAsync();

        if (entitiesToRemove.Any()) {
            _dbContext.RemoveRange(entitiesToRemove);
        }

        // add new subscriptions
        var entitiesToAdd = caseTypeIds.Select(id => new DbNotificationSubscription {
            CaseTypeId = id,
            Subscriber = subscriber.Clone()
        });

        if (entitiesToAdd.Any()) {
            await _dbContext.AddRangeAsync(entitiesToAdd);
        }

        await _dbContext.SaveChangesAsync();
    }

}