﻿using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq.Expressions;
using System.Security.Claims;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Indice.Security;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Services;

internal class CaseTypeService : ICaseTypeService
{
    private readonly CasesDbContext _dbContext;

    public CaseTypeService(CasesDbContext dbContext) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<DbCaseType> Get(string code) {
        if (string.IsNullOrEmpty(code)) {
            throw new ArgumentNullException(nameof(code));
        }
        var caseType = await _dbContext.CaseTypes
            .AsQueryable()
            .FirstOrDefaultAsync(p => p.Code == code);
        return caseType ?? throw new Exception("CaseType is invalid."); // todo proper exception;
    }

    public async Task<DbCaseType> Get(Guid id) {
        if (id == Guid.Empty) {
            throw new ArgumentNullException(nameof(id));
        }
        var caseType = await _dbContext.CaseTypes.FindAsync(id);
        return caseType ?? throw new Exception("CaseType is invalid."); // todo proper exception;
    }

    public async Task<ResultSet<CaseTypePartial>> Get(ClaimsPrincipal user, bool canCreate) {
        if (user.IsAdmin()) {
            return await GetAdminCaseTypes();
        }

        var roleClaims = user.Claims
            .Where(c => c.Type == BasicClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        var caseTypeIds = canCreate ? await GetCaseTypeIdsForCaseCreation(roleClaims) : await GetCaseTypeIds(roleClaims);

        var caseTypes = await _dbContext.CaseTypes
            .AsQueryable()
            .Where(c => caseTypeIds.Contains(c.Id))
            .OrderBy(c => c.Category == null ? null : c.Category.Order)
            .ThenBy(c => c.Order)
            .Select(c => new CaseTypePartial {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                Category = c.Category == null ? null : new Category {
                    Id = c.Category.Id,
                    Name = c.Category.Name,
                    Description = c.Category.Description,
                    Order = c.Category.Order,
                    Translations = TranslationDictionary<CategoryTranslation>.FromJson(c.Category.Translations)
                },
                DataSchema = c.DataSchema,
                Layout = c.Layout,
                Code = c.Code,
                Order = c.Order,
                Tags = c.Tags,
                Config = c.Config,
                Translations = TranslationDictionary<CaseTypeTranslation>.FromJson(c.Translations),
                GridFilterConfig = c.GridFilterConfig,
                GridColumnConfig = c.GridColumnConfig,
                IsMenuItem = c.IsMenuItem,
            })
            .ToListAsync();
        TranslateCaseTypes(caseTypes);
        return caseTypes.ToResultSet();
    }

    public async Task Create(CaseTypeRequest caseType) {
        var codeExists = await CaseTypeCodeExists(caseType.Code);
        if (codeExists) {
            throw new ValidationException("Case type code already exists.");
        }

        var newCaseType = new DbCaseType {
            Id = Guid.NewGuid(),
            Code = caseType.Code,
            Title = caseType.Title,
            Description = caseType.Description,
            DataSchema = caseType.DataSchema,
            Layout = caseType.Layout,
            Translations = caseType.Translations,
            LayoutTranslations = caseType.LayoutTranslations,
            Tags = caseType.Tags,
            Config = caseType.Config,
            CanCreateRoles = caseType.CanCreateRoles,
            Order = caseType.Order,
            IsMenuItem = caseType.IsMenuItem,
            GridFilterConfig = caseType.GridFilterConfig,
            GridColumnConfig = caseType.GridColumnConfig,
        };

        await _dbContext.CaseTypes.AddAsync(newCaseType);
        await _dbContext.SaveChangesAsync();
    }

    public async Task Delete(Guid caseTypeId) {
        if (caseTypeId == Guid.Empty) {
            throw new ValidationException("Case Type id not provided.");
        }
        var casesWithCaseType = await _dbContext.Cases.AsQueryable().AnyAsync(x => x.CaseTypeId == caseTypeId);
        if (casesWithCaseType) {
            throw new ValidationException("Case type cannot be deleted because there are cases with this type.");
        }
        var roleCaseTypes = await _dbContext.CaseMembers.AsQueryable().Where(x => x.RuleCaseTypeId == caseTypeId).ToListAsync();
        if (roleCaseTypes.Any()) {
            roleCaseTypes.ForEach(x => _dbContext.CaseMembers.Remove(x));
        }
        var checkpointTypes = await _dbContext.CheckpointTypes.AsQueryable().Where(x => x.CaseTypeId == caseTypeId).ToListAsync();
        if (checkpointTypes.Any()) {
            checkpointTypes.ForEach(x => _dbContext.CheckpointTypes.Remove(x));
        }
        var dbCaseType = await Get(caseTypeId);
        _dbContext.CaseTypes.Remove(dbCaseType);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<CaseType> GetCaseTypeDetailsById(Guid id) {
        var dbCaseType = await _dbContext.CaseTypes
            .AsNoTracking()
            .Include(x => x.CheckpointTypes)
            .FirstOrDefaultAsync(p => p.Id == id);

        var caseTypeRoles = await _dbContext.CaseMembers.AsQueryable()
            .Where(p => p.RuleCaseTypeId == id)
            .ToListAsync();

        var caseType = new CaseType {
            Id = id,
            Code = dbCaseType.Code,
            Title = dbCaseType.Title,
            Description = dbCaseType.Description,
            DataSchema = dbCaseType.DataSchema,
            Layout = dbCaseType.Layout,
            Translations = dbCaseType.Translations,
            LayoutTranslations = dbCaseType.LayoutTranslations,
            Tags = dbCaseType.Tags,
            Config = dbCaseType.Config,
            CanCreateRoles = dbCaseType.CanCreateRoles,
            Order = dbCaseType.Order,
            CheckpointTypes = dbCaseType.CheckpointTypes?.Select(checkpointType => new CheckpointTypeDetails {
                Id = checkpointType.Id,
                Code = checkpointType.Code,
                Description = checkpointType.Description,
                Private = checkpointType.Private,
                Status = checkpointType.Status,
                Roles = caseTypeRoles
                    .Where(roleCaseType => roleCaseType.RuleCheckpointTypeId == checkpointType.Id)
                    .Select(roleCaseType => roleCaseType.MemberRole)
            }),
            IsMenuItem = dbCaseType.IsMenuItem,
            GridColumnConfig = dbCaseType.GridColumnConfig,
            GridFilterConfig = dbCaseType.GridFilterConfig,
        };

        return caseType;
    }

    public async Task<CaseType> Update(CaseTypeRequest caseType) {
        if (!caseType.Id.HasValue) {
            throw new ValidationException("Case type can not be null.");
        }
        var dbCaseType = await Get(caseType.Id.Value);
        if (dbCaseType.Code != caseType.Code) {
            throw new ValidationException("Case type code cannot be changed.");
        }

        // Update case type entity
        dbCaseType.Title = caseType.Title;
        dbCaseType.Description = caseType.Description;
        dbCaseType.DataSchema = caseType.DataSchema;
        dbCaseType.Layout = caseType.Layout;
        dbCaseType.Translations = caseType.Translations;
        dbCaseType.LayoutTranslations = caseType.LayoutTranslations;
        dbCaseType.Tags = caseType.Tags;
        dbCaseType.Config = caseType.Config;
        dbCaseType.CanCreateRoles = caseType.CanCreateRoles;
        dbCaseType.Order = caseType.Order;
        dbCaseType.IsMenuItem = caseType.IsMenuItem;
        dbCaseType.GridColumnConfig = caseType.GridColumnConfig;
        dbCaseType.GridFilterConfig = caseType.GridFilterConfig;
        dbCaseType.IsMenuItem = caseType.IsMenuItem;

        _dbContext.CaseTypes.Update(dbCaseType);

        await _dbContext.SaveChangesAsync();
        return await GetCaseTypeDetailsById(caseType.Id.Value);
    }

    private async Task<bool> CaseTypeCodeExists(string caseTypeCode) {
        return await _dbContext.CaseTypes.AsQueryable().AnyAsync(c => c.Code == caseTypeCode);
    }

    private async Task<ResultSet<CaseTypePartial>> GetAdminCaseTypes() {
        var caseTypes = await _dbContext.CaseTypes
            .AsQueryable()
                .OrderBy(c => c.Category == null ? null : c.Category.Order)
                .ThenBy(c => c.Order)
                .Select(c => new CaseTypePartial {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    Category = c.Category == null ? null : new Category {
                        Id = c.Category.Id,
                        Name = c.Category.Name,
                        Description = c.Category.Description,
                        Order = c.Category.Order,
                        Translations = TranslationDictionary<CategoryTranslation>.FromJson(c.Category.Translations)
                    },
                    Code = c.Code,
                    Tags = c.Tags,
                    Order = c.Order,
                    Translations = TranslationDictionary<CaseTypeTranslation>.FromJson(c.Translations),
                    GridFilterConfig = c.GridFilterConfig,
                    GridColumnConfig = c.GridColumnConfig,
                    IsMenuItem = c.IsMenuItem
                })
                .ToListAsync();
        TranslateCaseTypes(caseTypes);
        return caseTypes.ToResultSet();
    }

    private void TranslateCaseTypes(List<CaseTypePartial> caseTypes) {
        for (var i = 0; i < caseTypes.Count; i++) {
            caseTypes[i] = caseTypes[i].Translate(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, true);
            if (caseTypes[i].Category is not null) {
                caseTypes[i].Category = TranslateCaseTypeCategory(caseTypes[i].Category, CultureInfo.CurrentCulture.TwoLetterISOLanguageName, true);
            }
        }
    }

    private Category TranslateCaseTypeCategory(Category category, string culture, bool includeTranslations) =>
    category.Translate(culture, includeTranslations);

    private async Task<List<Guid>> GetCaseTypeIdsForCaseCreation(List<string> roleClaims) {
        var caseTypeExpressions = roleClaims.Select(roleClaim => (Expression<Func<DbCaseType, bool>>)(dbCaseType => EF.Functions.Like(dbCaseType.CanCreateRoles, $"%{roleClaim}%")));
        // Aggregate the expressions with OR that resolves to SQL: CanCreateRoles LIKE %roleClaim1% OR tag LIKE %roleClaim2% etc
        var aggregatedExpression = caseTypeExpressions.Aggregate((expression, next) => {
            var orExp = Expression.OrElse(expression.Body, Expression.Invoke(next, expression.Parameters));
            return Expression.Lambda<Func<DbCaseType, bool>>(orExp, expression.Parameters);
        });

        return await _dbContext.CaseTypes
            .AsQueryable()
            .Where(aggregatedExpression)
            .Select(c => c.Id)
            .ToListAsync();
    }

    private async Task<List<Guid>> GetCaseTypeIds(List<string> roleClaims) {
        return await _dbContext.CaseMembers
                .AsQueryable()
                .Where(r => roleClaims.Contains(r.MemberRole) && r.RuleCaseTypeId != null)
                .Select(c => c.RuleCaseTypeId.Value)
                .ToListAsync();
    }

}