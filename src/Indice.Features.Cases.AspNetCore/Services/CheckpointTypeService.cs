using System.Globalization;
using System.Security.Claims;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models.Responses;
using Indice.Security;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Services;

internal class CheckpointTypeService : ICheckpointTypeService
{
    private readonly CasesDbContext _dbContext;

    public CheckpointTypeService(CasesDbContext dbContext) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IEnumerable<CheckpointType>> GetDistinctCheckpointTypes(ClaimsPrincipal user) {
        if (user.IsAdmin()) {
            return await GetAdminDistinctCheckpoints();
        }

        var roleClaims = user.Claims
            .Where(c => c.Type == BasicClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        var checkpointTypeIds = await _dbContext.Members
            .AsQueryable()
            .Where(r => roleClaims.Contains(r.RoleName))
            .Select(c => c.CheckpointTypeId)
            .ToListAsync();

        var checkpointTypes = await (
                from c in _dbContext.CheckpointTypes
                where checkpointTypeIds.Contains(c.Id)
                group c by new { c.Code, c.Title } into grouped
                select grouped.FirstOrDefault()
            ).ToListAsync();

        return TranslateCheckpoints(checkpointTypes);
    }

    private async Task<IEnumerable<CheckpointType>> GetAdminDistinctCheckpoints() {
        var checkpointTypes = await (
            from c in _dbContext.CheckpointTypes
            group c by new { c.Code, c.Title } into grouped
            select grouped.FirstOrDefault()
        ).ToListAsync();

        return TranslateCheckpoints(checkpointTypes);
    }

    private static IEnumerable<CheckpointType> TranslateCheckpoints(IEnumerable<DbCheckpointType> checkpointTypes) {
        var translated = checkpointTypes
            .Select(c => new CheckpointType {
                Id = c.Id,
                Code = c.Code,
                Title = c.Title,
                Description = c.Description,
                Translations = TranslationDictionary<CheckpointTypeTranslation>.FromJson(c.Translations)
            })
            .ToList();

        foreach (var item in translated) {
            var translation = item.Translate(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, false);
            item.Title = translation.Title ?? item.Title;
            item.Description = translation.Description ?? item.Description;
        }

        return translated;
    }
}