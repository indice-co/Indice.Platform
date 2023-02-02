using System.Globalization;
using System.Security.Claims;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Services
{
    internal class NotificationSubscriptionService : INotificationSubscriptionService
    {
        private readonly CasesDbContext _dbContext;
        private readonly ICaseTypeService _caseTypeService;

        public NotificationSubscriptionService(
            CasesDbContext dbContext,
            ICaseTypeService caseTypeService) {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _caseTypeService = caseTypeService ?? throw new ArgumentNullException(nameof(caseTypeService));
        }

        public async Task<NotificationSubscriptionDTO> GetSubscriptions(ClaimsPrincipal user, ListOptions<NotificationFilter> options) {
            // what case types is the user allowed to see?
            var caseTypes = await _caseTypeService.Get(user, false);
            // fetch active Subscriptions
            var filter = options.Filter ?? new NotificationFilter();
            var subscriptions = await _dbContext.NotificationSubscriptions
                .AsQueryable()
                .Where(x => (filter.Email.Length == 0 || filter.Email.Contains(x.Email)) &&
                            (filter.GroupId.Length == 0 || filter.GroupId.Contains(x.GroupId)))
                .Select(x => new NotificationSubscriptionSetting {
                    Subscribed = true,
                    CaseType = new CaseTypePartial {
                        Id = x.CaseType.Id,
                        Title = x.CaseType.Title,
                        Translations = TranslationDictionary<CaseTypeTranslation>.FromJson(x.CaseType.Translations)
                    }
                })
                .ToListAsync();
            // add "inactive Subscriptions"
            foreach (CaseTypePartial c in caseTypes.Items) {
                if (!subscriptions.Any(s => s.CaseType.Id == c.Id)) {
                    subscriptions.Add(new NotificationSubscriptionSetting {
                        Subscribed = false,
                        CaseType = new CaseTypePartial {
                            Id = c.Id,
                            Title = c.Title
                        }
                    });
                }
            }
            // translate subscriptions' case types
            for (var i = 0; i < subscriptions.Count; i++) {
                subscriptions[i].CaseType = subscriptions[i].CaseType.Translate(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, true);
            }
            // finally, return result
            return new NotificationSubscriptionDTO {
                NotificationSubscriptionSettings = subscriptions
            };
        }

        public async Task Subscribe(List<NotificationSubscriptionSetting> settings, NotificationSubscription subscriber) {
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
            settings.ForEach(x => {
                if (x.Subscribed) {
                    entitiesToAdd.Add(new DbNotificationSubscription {
                        CaseTypeId = x.CaseType.Id,
                        GroupId = subscriber.GroupId,
                        Email = subscriber.Email
                    });
                }
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