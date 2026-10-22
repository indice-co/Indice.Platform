using System.Globalization;
using System.Security.Claims;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Security;
using Microsoft.EntityFrameworkCore;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Requests;
using Indice.Types;
using Microsoft.Data.SqlClient;

namespace Indice.Features.Cases.Core.Services;

internal class CheckpointTypeService : ICheckpointTypeService
{
    private const string RequiredCheckpointTypeCode = "Submitted";
    private readonly CasesDbContext _dbContext;

    public CheckpointTypeService(CasesDbContext dbContext) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<List<CheckpointType>> GetDistinctCheckpointTypes(UserActor user) => await GetDistinctCheckpointTypes(user, null);

    private async Task<List<CheckpointType>> GetDistinctCheckpointTypes(UserActor user, Guid? caseTypeId) {
        if (user.IsAdmin) {
            return await GetAdminDistinctCheckpointsTypes(caseTypeId);
        }

        var accessRules = _dbContext.CaseAccessRules
            .AsQueryable()
            .Where(DbCaseAccessRule.AccessMatchPredicate(user.Id, user.Roles, user.GroupId));

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
                // the user can see a checkpoint type either through a rule on the checkpoint type itself or through a rule on its case type
                where accessRules.Any(r => r.RuleCheckpointTypeId == c.Id || r.RuleCaseTypeId == c.CaseTypeId)
                where caseTypeId == null || c.CaseTypeId == caseTypeId
                group c by c.Code into grouped
                select grouped.FirstOrDefault()
            ).ToListAsync();

        return TranslateCheckpointTypes(checkpointTypes);
    }

    private async Task<List<CheckpointType>> GetAdminDistinctCheckpointsTypes(Guid? caseTypeId) {

        // Please check the comments above regarding the logic behind this query 
        var checkpointTypes = await (
            from c in _dbContext.CheckpointTypes
            where caseTypeId == null || c.CaseTypeId == caseTypeId
            group c by c.Code into grouped
            select grouped.FirstOrDefault()
        ).ToListAsync();

        return TranslateCheckpointTypes(checkpointTypes);
    }

    public async Task<ResultSet<CheckpointType>> GetList(UserActor user, ListOptions<GetCheckpointTypesListFilter> options) {
        var filter = options.Filter ?? new GetCheckpointTypesListFilter();
        if (filter.Distinct == true) {
            return (await GetDistinctCheckpointTypes(user, filter.CaseTypeId)).ToResultSet();
        }

        var query = _dbContext.CheckpointTypes.AsNoTracking();
        if (!user.IsAdmin) {
            var accessRules = _dbContext.CaseAccessRules
                .AsQueryable()
                .Where(DbCaseAccessRule.AccessMatchPredicate(user.Id, user.Roles, user.GroupId));
            query = query.Where(c => accessRules.Any(r => r.RuleCheckpointTypeId == c.Id || r.RuleCaseTypeId == c.CaseTypeId));
        }
        var caseTypeId = filter.CaseTypeId;
        if (caseTypeId is Guid value) {
            query = query.Where(c => c.CaseTypeId == value);
        }
        if (!string.IsNullOrWhiteSpace(options.Search)) {
            var search = options.Search.Trim();
            query = query.Where(c => c.Code.Contains(search) || (c.Title != null && c.Title.Contains(search)) || c.CaseType.Code.Contains(search));
        }
        if (string.IsNullOrWhiteSpace(options.Sort)) {
            options.Sort = $"{nameof(CheckpointType.CaseTypeCode)},{nameof(CheckpointType.Code)}";
        }
        var result = await query.Select(ToModel).ToResultSetAsync(options);
        foreach (var item in result.Items) {
            Translate(item);
        }
        return result;
    }

    public async Task<CheckpointType?> GetById(Guid id) {
        var item = await _dbContext.CheckpointTypes.AsNoTracking().Where(x => x.Id == id).Select(ToModel).FirstOrDefaultAsync();
        return item;
    }

    public async Task<CheckpointType> Create(CheckpointTypeRequest request) {
        if (!await _dbContext.CaseTypes.AnyAsync(x => x.Id == request.CaseTypeId)) {
            throw new BusinessException("Case type does not exist.", nameof(request.CaseTypeId));
        }
        if (await _dbContext.CheckpointTypes.AnyAsync(x => x.CaseTypeId == request.CaseTypeId && x.Code == request.Code)) {
            throw new BusinessException("A checkpoint type with the same code already exists for this case type.", nameof(request.Code));
        }
        var entity = new DbCheckpointType { Id = Guid.NewGuid() };
        Map(request, entity);
        _dbContext.CheckpointTypes.Add(entity);
        try {
            await _dbContext.SaveChangesAsync();
        } catch (DbUpdateException ex) when (IsDuplicateCheckpointCodeViolation(ex)) {
            throw DuplicateCheckpointCodeException();
        }
        return (await GetById(entity.Id))!;
    }

    public async Task<CheckpointType?> Update(Guid id, CheckpointTypeRequest request) {
        var entity = await _dbContext.CheckpointTypes.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) {
            return null;
        }
        if (string.Equals(entity.Code, RequiredCheckpointTypeCode, StringComparison.OrdinalIgnoreCase) &&
            (!string.Equals(entity.Code, request.Code, StringComparison.Ordinal) || entity.CaseTypeId != request.CaseTypeId)) {
            throw new BusinessException("The required Submitted checkpoint type cannot be renamed or moved.");
        }
        if (entity.CaseTypeId != request.CaseTypeId) {
            if (!await _dbContext.CaseTypes.AnyAsync(x => x.Id == request.CaseTypeId)) {
                throw new BusinessException("Case type does not exist.", nameof(request.CaseTypeId));
            }
            if (await _dbContext.Checkpoints.AnyAsync(x => x.CheckpointTypeId == id)) {
                throw new BusinessException("Checkpoint type cannot be moved to another case type because it is used by existing cases.", nameof(request.CaseTypeId));
            }
        }
        if (await _dbContext.CheckpointTypes.AnyAsync(x => x.Id != id && x.CaseTypeId == request.CaseTypeId && x.Code == request.Code)) {
            throw new BusinessException("A checkpoint type with the same code already exists for this case type.", nameof(request.Code));
        }
        if (entity.Private != request.Private && await _dbContext.Checkpoints.AnyAsync(x => x.CheckpointTypeId == id)) {
            throw new BusinessException("Checkpoint type visibility cannot be changed because it is used by existing cases.", nameof(request.Private));
        }
        if (entity.Status != request.Status && await _dbContext.Checkpoints.AnyAsync(x => x.CheckpointTypeId == id)) {
            throw new BusinessException("Checkpoint type status cannot be changed because it is used by existing cases.", nameof(request.Status));
        }
        Map(request, entity);
        try {
            await _dbContext.SaveChangesAsync();
        } catch (DbUpdateException ex) when (IsDuplicateCheckpointCodeViolation(ex)) {
            throw DuplicateCheckpointCodeException();
        }
        return await GetById(id);
    }

    public async Task<bool> Delete(Guid id) {
        var entity = await _dbContext.CheckpointTypes.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) {
            return false;
        }
        if (string.Equals(entity.Code, RequiredCheckpointTypeCode, StringComparison.OrdinalIgnoreCase)) {
            throw new BusinessException("The required Submitted checkpoint type cannot be deleted.", nameof(id));
        }
        if (await _dbContext.Checkpoints.AnyAsync(x => x.CheckpointTypeId == id)) {
            throw new BusinessException("Checkpoint type cannot be deleted because it is used by existing cases.", nameof(id));
        }
        var rules = await _dbContext.CaseAccessRules.Where(x => x.RuleCheckpointTypeId == id).ToListAsync();
        _dbContext.CaseAccessRules.RemoveRange(rules);
        _dbContext.CheckpointTypes.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    private static bool IsDuplicateCheckpointCodeViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 } sqlException &&
        sqlException.Message.Contains("IX_CheckpointType_CaseTypeId_Code", StringComparison.OrdinalIgnoreCase);

    private static BusinessException DuplicateCheckpointCodeException() =>
        new("A checkpoint type with the same code already exists for this case type.", nameof(CheckpointTypeRequest.Code));

    private static void Map(CheckpointTypeRequest request, DbCheckpointType entity) {
        entity.CaseTypeId = request.CaseTypeId;
        entity.Code = request.Code;
        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.Status = request.Status;
        entity.Private = request.Private;
        entity.Translations = request.Translations;
    }

    private static readonly System.Linq.Expressions.Expression<Func<DbCheckpointType, CheckpointType>> ToModel = c => new CheckpointType {
        Id = c.Id,
        CaseTypeId = c.CaseTypeId,
        CaseTypeCode = c.CaseType.Code,
        Code = c.Code,
        Title = c.Title,
        Description = c.Description,
        Status = c.Status,
        Private = c.Private,
        Translations = c.Translations
    };

    private static void Translate(CheckpointType item) {
        var translation = item.Translate(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, false);
        item.Title = translation.Title ?? item.Title;
        item.Description = translation.Description ?? item.Description;
    }

    private static List<CheckpointType> TranslateCheckpointTypes(IEnumerable<DbCheckpointType> checkpointTypes) {
        var translated = checkpointTypes
            .Select(c => new CheckpointType {
                Id = c.Id,
                Code = c.Code,
                Title = c.Title,
                Description = c.Description,
                Translations = c.Translations
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