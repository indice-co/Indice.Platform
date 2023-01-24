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

        public NotificationSubscriptionService(
            CasesDbContext dbContext) {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<IEnumerable<NotificationSubscription>> GetSubscribersByGroupId(string groupId) {
            return await _dbContext.NotificationSubscriptions
                .AsQueryable()
                .Where(x => x.GroupId == groupId)
                .Select(x => new NotificationSubscription {
                    Email = x.Email,
                    GroupId = x.GroupId
                })
                .ToListAsync();
        }

        public async Task Subscribe(NotificationSubscription subscriber) {
            if (string.IsNullOrEmpty(subscriber.GroupId)) throw new ArgumentException($"No Group found for subscriber: \"{subscriber.Email}\".");
            if (string.IsNullOrEmpty(subscriber.Email)) throw new ArgumentNullException(nameof(subscriber.Email));

            var entitiesToRemove = await _dbContext.NotificationSubscriptions
                .AsQueryable()
                .Where(u => u.Email == subscriber.Email)
                .ToListAsync();
            if (entitiesToRemove != null && entitiesToRemove.Count() > 0) {
                _dbContext.RemoveRange(entitiesToRemove);
            }
            var entity = new DbNotificationSubscription {
                GroupId = subscriber.GroupId,
                Email = subscriber.Email
            };
            await _dbContext.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> GetSubscriptions(ListOptions<NotificationFilter> options) {
            var filter = options.Filter ?? new NotificationFilter();
            return await _dbContext.NotificationSubscriptions
                .AsQueryable()
                .Where(x => (filter.Email.Length == 0 || filter.Email.Contains(x.Email)) &&
                            (filter.GroupId.Length == 0 || filter.GroupId.Contains(x.GroupId)))
                .AnyAsync();
        }

        public async Task Unsubscribe(NotificationFilter criteria) {
            if (criteria is null) {
                throw new ArgumentNullException(nameof(criteria));
            }
            var entitiesToRemove = await _dbContext.NotificationSubscriptions
                .AsQueryable()
                .Where(x => (criteria.Email.Length == 0 || criteria.Email.Contains(x.Email)) &&
                            (criteria.GroupId.Length == 0 || criteria.GroupId.Contains(x.GroupId)))
                .ToListAsync();
            if (entitiesToRemove.Any()) {
                _dbContext.RemoveRange(entitiesToRemove);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}