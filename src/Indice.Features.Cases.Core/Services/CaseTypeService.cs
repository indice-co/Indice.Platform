﻿using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq.Expressions;
using System.Security.Claims;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Security;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Data.Models;
using System.Text.RegularExpressions;

namespace Indice.Features.Cases.Core.Services;

internal class CaseTypeService : ICaseTypeService
{
    private readonly CasesDbContext _dbContext;

    public CaseTypeService(CasesDbContext dbContext) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<CaseType> Get(string code) {
        if (string.IsNullOrEmpty(code)) {
            throw new ArgumentNullException(nameof(code));
        }
        var caseType = await _dbContext.CaseTypes
            .Where(x => x.Code == code)
            .Select(x => new CaseType { 
                Id = x.Id,
                CanCreateRoles = x.CanCreateRoles,
                Code = x.Code,
                CheckpointTypes = x.CheckpointTypes.Select(c => new Models.CheckpointTypeDetails { 
                    Code = c.Code,
                    Id = c.Id,
                    Description = c.Description,
                    Private = c.Private,
                    Status = c.Status
                }).ToList(),
                Config = x.Config,
                DataSchema = x.DataSchema,
                Description = x.Description,
                GridColumnConfig = x.GridColumnConfig,
                GridFilterConfig = x.GridFilterConfig,
                IsMenuItem = x.IsMenuItem,
                Layout = x.Layout,
                LayoutTranslations = x.LayoutTranslations,
                Order = x.Order,
                Tags = x.Tags,
                Title = x.Title!,
                Translations = x.Translations
            })
            .FirstOrDefaultAsync();
        return caseType ?? throw new Exception("CaseType is invalid."); // todo proper exception;
    }

    public async Task<CaseType> Get(Guid id) {
        if (id == Guid.Empty) {
            throw new ArgumentNullException(nameof(id));
        }
        return await GetCaseTypeDetailsById(id);
    }

    public async Task<ResultSet<CaseTypePartial>> Get(ClaimsPrincipal user, bool canCreate) {
        if (user.IsAdmin()) {
            return await GetAdminCaseTypes(canCreate);
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
                    Name = c.Category.Name!,
                    Description = c.Category.Description,
                    Order = c.Category.Order,
                    Translations = c.Category.Translations
                },
                DataSchema = c.DataSchema,
                Layout = c.Layout,
                Code = c.Code,
                Order = c.Order,
                Tags = c.Tags,
                Config = c.Config,
                Translations = c.Translations,
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
        var roleCaseTypes = await _dbContext.CaseAccessRules.AsQueryable().Where(x => x.RuleCaseTypeId == caseTypeId).ToListAsync();
        if (roleCaseTypes.Any()) {
            roleCaseTypes.ForEach(x => _dbContext.CaseAccessRules.Remove(x));
        }

        var accessRulecheckpointTypes = await (
                                         from rule in _dbContext.CaseAccessRules
                                         join checkpointType in _dbContext.CheckpointTypes
                                             on rule.RuleCheckpointTypeId equals checkpointType.Id
                                         where checkpointType.CaseTypeId == caseTypeId
                                         select rule
                                         ).ToListAsync();
        if (accessRulecheckpointTypes.Any()) {
            accessRulecheckpointTypes.ForEach(x => _dbContext.CaseAccessRules.Remove(x));
        }

        var checkpointTypes = await _dbContext.CheckpointTypes.AsQueryable().Where(x => x.CaseTypeId == caseTypeId).ToListAsync();
        if (checkpointTypes.Any()) {
            checkpointTypes.ForEach(x => _dbContext.CheckpointTypes.Remove(x));
        }
        var dbCaseType = await _dbContext.CaseTypes.Where(x => x.Id == caseTypeId).FirstAsync();
        _dbContext.CaseTypes.Remove(dbCaseType);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<CaseType> GetCaseTypeDetailsById(Guid caseTypeId) {
        var dbCaseType = await _dbContext.CaseTypes
            .AsNoTracking()
            .Include(x => x.CheckpointTypes)
            .FirstOrDefaultAsync(p => p.Id == caseTypeId);

        if (dbCaseType is null) {
            throw new Exception("CaseType is invalid.");
        } 

        var caseTypeRoles = await _dbContext.CaseAccessRules
                            .AsNoTracking()
                            .Where(p => p.RuleCaseTypeId == caseTypeId && !string.IsNullOrEmpty(p.MemberRole))
                            .ToListAsync();

        var caseCheckPointRoles = await _dbContext.CaseAccessRules
                                .AsNoTracking()
                                .Include(x => x.CheckpointType)
                                .Where(p => p.CheckpointType!.CaseTypeId == caseTypeId && !string.IsNullOrEmpty(p.MemberRole))
                                .ToListAsync();
        var caseType = new CaseType {
            Id = caseTypeId,
            Code = dbCaseType!.Code,
            Title = dbCaseType.Title!,
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
                Roles =
                caseTypeRoles.Select(roleCaseType => roleCaseType.MemberRole!)
                .Union(
                    caseCheckPointRoles
                    .Where(roleCaseType => roleCaseType.CheckpointType!.Id == checkpointType.Id)
                    .Select(roleCaseType => roleCaseType.MemberRole!)
                ).ToList()
            }).ToList() ?? [],
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
        var dbCaseType = await _dbContext.CaseTypes.FindAsync(caseType.Id);
        if (dbCaseType!.Code != caseType.Code) {
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

    private async Task<ResultSet<CaseTypePartial>> GetAdminCaseTypes(bool canCreate = false) {
        var caseTypes = await _dbContext.CaseTypes
            .AsQueryable()
                .OrderBy(c => c.Category == null ? null : c.Category.Order)
                .ThenBy(c => c.Order)
                //if canCreate is true => hide case types that can't be created from an agent
                //if canCreate is false => fetch all
                .Where(x => canCreate ? x.CanCreateRoles != null : true)
                .Select(c => new CaseTypePartial {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    Category = c.Category == null ? null : new Category {
                        Id = c.Category.Id,
                        Name = c.Category.Name!,
                        Description = c.Category.Description,
                        Order = c.Category.Order,
                        Translations = c.Category.Translations
                    },
                    Code = c.Code,
                    Tags = c.Tags,
                    Order = c.Order,
                    Translations = c.Translations,
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
                caseTypes[i].Category = caseTypes[i].Category!.Translate(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, includeTranslations: true);
            }
        }
    }

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
        return await _dbContext.CaseAccessRules
                .AsQueryable()
                .Where(r => r.RuleCaseTypeId.HasValue)
                .Where(r => roleClaims.Contains(r.MemberRole!))
                .Select(c => c.RuleCaseTypeId!.Value)
                .ToListAsync();
    }

}