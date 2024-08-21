using System.Globalization;
using System.Security.Claims;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models.Requests;
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

    public async Task<CheckpointType> CreateCheckpointType(CheckpointTypeRequest checkpointTypeRequest) {
        await CheckpointTypeBusinessValidation(checkpointTypeRequest.CaseTypeId, checkpointTypeRequest.Code);

        var dbCheckpointType = new DbCheckpointType() {
            CaseTypeId = checkpointTypeRequest.CaseTypeId,
            Code = checkpointTypeRequest.Code,
            Description = checkpointTypeRequest.Description,
            Private = checkpointTypeRequest.Private,
            Status = checkpointTypeRequest.Status,
            Title = checkpointTypeRequest.Title,
            Translations = checkpointTypeRequest.Translations
        };
        var result = await _dbContext.CheckpointTypes.AddAsync(dbCheckpointType);
        await _dbContext.SaveChangesAsync();

        return TranslateCheckpointTypes([result.Entity]).First();
    }

    public async Task<ResultSet<CheckpointType>> GetCaseTypeCheckpointTypes(ClaimsPrincipal user, Guid caseTypeId) {
        if (!user.IsAdmin()) {
            throw new Exception("User is not an admin.");
        }

        var checkpointQuery = _dbContext.CheckpointTypes.Where(x => x.CaseTypeId == caseTypeId);
        var result = await GetAdminDistinctCheckpointsTypes(checkpointQuery);
        return result.ToResultSet();
    }

    public async Task<GetCheckpointTypeResponse> GetCheckpointTypeById(Guid checkpointTypeId) {
        var result = await _dbContext.CheckpointTypes.FindAsync(checkpointTypeId);
        if (result == null) {
            throw new Exception("Checkpoint type with that id was not found.");
        }
        var translated = TranslateCheckpointTypes([result]).First();

        return new GetCheckpointTypeResponse() {
            Id = translated.Id,
            Code = translated.Code,
            Translations = translated.Translations?.ToJson(),
            Description = translated.Description,
            Private = translated.Private,
            Status = translated.Status,
            Title = translated.Title
        };
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

    public async Task<CheckpointType> EditCheckpointType(EditCheckpointTypeRequest editCheckpointTypeRequest) {
        var result = await _dbContext.CheckpointTypes.FindAsync(editCheckpointTypeRequest.CheckpointTypeId);
        if (result == null) {
            throw new Exception("Checkpoint type with that id was not found.");
        }

        await CheckpointTypeBusinessValidation(editCheckpointTypeRequest.CaseTypeId, editCheckpointTypeRequest.Code);

        result.Code = editCheckpointTypeRequest.Code;
        await _dbContext.SaveChangesAsync();
        return TranslateCheckpointTypes([result]).First();
    }

    private async Task CheckpointTypeBusinessValidation(Guid caseTypeId, string code) {
        //There should be at least one "Submitted" code for checkpoints in a case type
        var submittedExists = await _dbContext.CheckpointTypes.Where(x => x.CaseTypeId == caseTypeId)
                                                              .AnyAsync(x => x.Code == CaseStatus.Submitted.ToString());

        if (!submittedExists) {
            throw new Exception($"You must first create a Checkpoint type with code name {CaseStatus.Submitted}.");
        }

        //There should be no duplicate codes for checkpoints in a case type
        var codeAlreadyExists = await _dbContext.CheckpointTypes.Where(x => x.CaseTypeId == caseTypeId)
                                                                .AnyAsync(x => x.Code == code);

        if (codeAlreadyExists) {
            throw new Exception($"Checkpoint type with code name {code} already exists.");
        }
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

    private async Task<IEnumerable<CheckpointType>> GetAdminDistinctCheckpointsTypes(IQueryable<DbCheckpointType> checkpointQuery) {

        // Please check the comments above regarding the logic behind this query 
        var checkpointTypes = await (
            from c in checkpointQuery
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
                Translations = TranslationDictionary<CheckpointTypeTranslation>.FromJson(c.Translations),
                Private = c.Private,
                Status = c.Status
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
