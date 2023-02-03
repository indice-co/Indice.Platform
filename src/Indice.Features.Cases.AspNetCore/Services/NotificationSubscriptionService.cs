using Indice.Features.Cases.Data;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Services
{
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
                            (filter.GroupId.Length == 0 || filter.GroupId.Contains(x.GroupId)))
                .Select(x => new NotificationSubscription {
                    CaseTypeId = x.CaseTypeId
                })
                .ToListAsync();
            return subscriptions;
        }

        public async Task Subscribe(List<Guid> caseTypeIds, NotificationSubscription subscriber) {
            if (string.IsNullOrEmpty(subscriber.GroupId)) throw new ArgumentException($"No Group found for subscriber: \"{subscriber.Email}\".");
            if (string.IsNullOrEmpty(subscriber.Email)) throw new ArgumentNullException(nameof(subscriber.Email));

            // delete all existing subscriptions
            var entitiesToRemove = await _dbContext.NotificationSubscriptions
                .AsQueryable()
                .Where(u => u.Email == subscriber.Email)
                .ToListAsync();
            if (entitiesToRemove != null && entitiesToRemove.Count() > 0) {
                _dbContext.RemoveRange(entitiesToRemove);
            }

            // add new subscriptions
            var entitiesToAdd = new List<DbNotificationSubscription> { };
            caseTypeIds.ForEach(id => {
                entitiesToAdd.Add(new DbNotificationSubscription {
                    CaseTypeId = id,
                    GroupId = subscriber.GroupId,
                    Email = subscriber.Email
                });
            });

            if (entitiesToAdd.Count() > 0) {
                await _dbContext.AddRangeAsync(entitiesToAdd);
            }
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<NotificationSubscription>> GetSubscribers(Guid caseTypeId, string groupId) {
            return await _dbContext.NotificationSubscriptions
                .AsQueryable()
                .Where(x => x.GroupId == groupId && x.CaseTypeId == caseTypeId)
                .Select(x => new NotificationSubscription {
                    Email = x.Email,
                    GroupId = x.GroupId
                })
                .ToListAsync();
        }
    }
}