using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Services
{
    internal class CaseTypeNotificationSubscriptionService : ICaseTypeNotificationSubscriptionService
    {
        private readonly CasesDbContext _dbContext;
        private readonly CasesApiOptions _casesApiOptions;

        public CaseTypeNotificationSubscriptionService(
            CasesDbContext dbContext, 
            CasesApiOptions casesApiOptions) {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _casesApiOptions = casesApiOptions ?? throw new ArgumentNullException(nameof(casesApiOptions));
        }

        public async Task<IEnumerable<DbCaseTypeNotificationSubscription>> GetCaseTypeUsersByGroupId(string groupId) {
            return await _dbContext.CaseTypeNotificationSubscription
                .AsQueryable()
                .Where(user => user.GroupId == groupId)
                .ToListAsync();
        }

        public async Task CreateCaseTypeNotificationSubscription(ClaimsPrincipal user) {
            var groupId = user.FindFirstValue(_casesApiOptions.GroupIdClaimType);
            if (string.IsNullOrEmpty(groupId)) {
                throw new Exception($"No Group found for user: {user.Identity?.Name}");
            }
            var email = user.FindFirstValue(JwtClaimTypes.Email);
            var entitiesToRemove = await _dbContext.CaseTypeNotificationSubscription
                .AsQueryable()
                .Where(u => u.Email == email)
                .ToListAsync();
            if (entitiesToRemove != null && entitiesToRemove.Count() > 0) {
                _dbContext.RemoveRange(entitiesToRemove);
            }
            var entity = new DbCaseTypeNotificationSubscription {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                Email = email
            };
            await _dbContext.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> GetCaseTypeNotificationSubscriptionByUser(ClaimsPrincipal user) {
            var groupId = user.FindFirstValue(_casesApiOptions.GroupIdClaimType);
            if (string.IsNullOrEmpty(groupId)) {
                throw new Exception($"No Group found for user: {user?.Identity?.Name}");
            }
            var email = user.FindFirstValue(JwtClaimTypes.Email);
            return await _dbContext.CaseTypeNotificationSubscription
                .AsQueryable()
                .AnyAsync(p => p.Email == email && p.GroupId == groupId);
        }

        public async Task DeleteCaseTypeNotificationSubscriptionByUser(ClaimsPrincipal user) {
            var groupId = user.FindFirstValue(_casesApiOptions.GroupIdClaimType);
            if (string.IsNullOrEmpty(groupId)) {
                throw new Exception($"No Group found for user: {user?.Identity?.Name}");
            }
            var email = user.FindFirstValue(JwtClaimTypes.Email);
            var entitiesToRemove = await _dbContext.CaseTypeNotificationSubscription
                .AsQueryable()
                .Where(u => u.Email == email)
                .ToListAsync();
            if (entitiesToRemove.Any()) {
                _dbContext.RemoveRange(entitiesToRemove);
                await _dbContext.SaveChangesAsync();
            }
        }

    }
}