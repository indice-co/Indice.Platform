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
            return await GetAdminDistinctCheckpointsTypes();
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

        /*
         * The logic behind this query is:
         * Fetch all checkpoint types grouped by Code (eg Submitted, Completed).                  
         *
         * The BO will show the grouped codes and will query against those codes.
         *
         * If, for "reasons", the business will require different Translations for, eg, "Completed",
         * the case types MUST be created with different Codes (eg Completed and Completed_B).
         * This way, the filter will be clear to the back-officer and to the query and will have
         * both values shown and filtered.
         */
        var checkpointTypes = await (
                from c in _dbContext.CheckpointTypes
                where checkpointTypeIds.Contains(c.Id)
                group c by c.Code into grouped
                select grouped.FirstOrDefault()
            ).ToListAsync();

        return TranslateCheckpointTypes(checkpointTypes);
    }

    private async Task<IEnumerable<CheckpointType>> GetAdminDistinctCheckpointsTypes() {

        // Please check the comments above regarding the logic behind this query 
        var checkpointTypes = await (
            from c in _dbContext.CheckpointTypes
            group c by c.Code into grouped
            select grouped.FirstOrDefault()
        ).ToListAsync();

        return TranslateCheckpointTypes(checkpointTypes);
    }

    private static IEnumerable<CheckpointType> TranslateCheckpointTypes(IEnumerable<DbCheckpointType> checkpointTypes) {
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