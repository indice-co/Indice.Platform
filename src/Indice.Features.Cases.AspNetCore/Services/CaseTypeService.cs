using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Indice.Security;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Services
{
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

        public async Task<ResultSet<CaseTypePartial>> Get(ClaimsPrincipal user, Data.Models.Action action) {
            if (user.IsAdmin()) {
                return await GetAdminCases();
            }

            var roleClaims = user.Claims
                .Where(c => c.Type == JwtClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            //if (forCreation) {
            //    var creationCaseTypes = await _dbContext.CaseTypes
            //                        .AsQueryable()
            //    .Select(c => new CaseTypePartial {
            //        Id = c.Id,
            //        Title = c.Title,
            //        DataSchema = c.DataSchema,
            //        Layout = c.Layout,
            //        Code = c.Code,
            //        Tags = c.Tags,
            //        AllowedRolesForCreation = string.IsNullOrWhiteSpace(c.AllowedRolesForCreation) ?
            //                                    new List<string>() :
            //                                    c.AllowedRolesForCreation!.Split(',', StringSplitOptions.None).ToList(),
            //        Translations = TranslationDictionary<CaseTypeTranslation>.FromJson(c.Translations)
            //    })
            //    .ToListAsync();

            //    return creationCaseTypes
            //        .Where(c => c.AllowedRolesForCreation.Intersect(roleClaims).Any())
            //        .ToResultSet();
            //}

            var caseTypeIds = await _dbContext.RoleCaseTypes
                .AsQueryable()
                .Where(r => roleClaims.Contains(r.RoleName) && r.Action.Contains(action.ToString()))
                .Select(c => c.CaseTypeId)
                .ToListAsync();

            var caseTypes = await _dbContext.CaseTypes
                .AsQueryable()
                .Where(c => caseTypeIds.Contains(c.Id))
                .Select(c => new CaseTypePartial {
                    Id = c.Id,
                    Title = c.Title,
                    DataSchema = c.DataSchema,
                    Layout = c.Layout,
                    Code = c.Code,
                    Tags = c.Tags,
                    Translations = TranslationDictionary<CaseTypeTranslation>.FromJson(c.Translations)
                })
                .ToListAsync();

            // translate case types
            for (var i = 0; i < caseTypes.Count; i++) {
                caseTypes[i] = caseTypes[i].Translate(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, true);
            }

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
                DataSchema = caseType.DataSchema,
                Layout = caseType.Layout,
                Translations = caseType.Translations,
                LayoutTranslations = caseType.LayoutTranslations,
                Tags = caseType.Tags
            };

            if (caseType.CheckpointTypes is null) {
                throw new ValidationException("At least one checkpoint type is required.");
            }

            var checkpointSubmittedExists = caseType.CheckpointTypes.Any(x => x.Name == "Submitted");
            if (!checkpointSubmittedExists) {
                throw new ValidationException("At least one checkpoint type with the name 'Submitted' is required.");
            }

            var checkpointNames = caseType.CheckpointTypes.Select(x => x.Name).ToList();
            if (checkpointNames.Count != checkpointNames.Distinct().Count()) {
                throw new ValidationException("You can't have duplicate names in checkpoint types.");
            }

            foreach (var checkpointType in caseType.CheckpointTypes) {
                var dbCheckpointType = new DbCheckpointType {
                    Id = Guid.NewGuid(),
                    CaseTypeId = newCaseType.Id,
                    Description = checkpointType.Description,
                    PublicStatus = checkpointType.PublicStatus,
                    Private = checkpointType.Private
                };
                dbCheckpointType.SetCode(caseType.Code, checkpointType.Name);

                await _dbContext.CheckpointTypes.AddAsync(dbCheckpointType);

                if (checkpointType.Roles?.Any() ?? false) {
                    foreach (var role in checkpointType.Roles!) {
                        await _dbContext.RoleCaseTypes.AddAsync(new DbRoleCaseType {
                            CaseTypeId = newCaseType.Id,
                            CheckpointTypeId = dbCheckpointType.Id,
                            RoleName = role
                        });
                    }
                }
            }

            await _dbContext.CaseTypes.AddAsync(newCaseType);
            await _dbContext.SaveChangesAsync();
        }

        public async Task Delete(Guid caseTypeId) {
            if (caseTypeId == null) {
                throw new ValidationException("Case Type id not provided.");
            }
            var casesWithCaseType = await _dbContext.Cases.AsQueryable().AnyAsync(x => x.CaseTypeId == caseTypeId);
            if (casesWithCaseType) {
                throw new ValidationException("Case type cannot be deleted because there are cases with this type.");
            }
            var roleCaseTypes = await _dbContext.RoleCaseTypes.AsQueryable().Where(x => x.CaseTypeId == caseTypeId).ToListAsync();
            if (roleCaseTypes.Any()) {
                roleCaseTypes.ForEach(x => _dbContext.RoleCaseTypes.Remove(x));
            }
            var checkpointTypes = await _dbContext.CheckpointTypes.AsQueryable().Where(x => x.CaseTypeId == caseTypeId).ToListAsync();
            if (checkpointTypes.Any()) {
                checkpointTypes.ForEach(x => _dbContext.CheckpointTypes.Remove(x));
            }
            var dbCaseType = await Get(caseTypeId);
            _dbContext.CaseTypes.Remove(dbCaseType);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<CaseTypeDetails> GetCaseTypeDetailsById(Guid id) {
            var dbCaseType = await _dbContext.CaseTypes
                .Include(p => p.CheckpointTypes)
                .FirstOrDefaultAsync(p => p.Id == id);

            var caseTypeRoles = await _dbContext.RoleCaseTypes.AsQueryable()
                .Where(p => p.CaseTypeId == id)
                .ToListAsync();

            var caseType = new CaseTypeDetails {
                Id = id,
                Code = dbCaseType.Code,
                Title = dbCaseType.Title,
                DataSchema = dbCaseType.DataSchema,
                Layout = dbCaseType.Layout,
                Translations = dbCaseType.Translations,
                LayoutTranslations = dbCaseType.LayoutTranslations,
                Tags = dbCaseType.Tags,
                CheckpointTypes = dbCaseType.CheckpointTypes.Select(checkpointType => new CheckpointTypeDetails {
                    Id = checkpointType.Id,
                    Name = checkpointType.Name,
                    Description = checkpointType.Description,
                    Private = checkpointType.Private,
                    PublicStatus = checkpointType.PublicStatus,
                    Roles = caseTypeRoles
                        .Where(roleCaseType => roleCaseType.CheckpointTypeId == checkpointType.Id)
                        .Select(roleCaseType => roleCaseType.RoleName)
                })
            };

            return caseType;
        }

        public async Task<CaseTypeDetails> Update(CaseTypeRequest caseType) {
            if (!caseType.Id.HasValue) {
                throw new ValidationException("Case type can not be null.");
            }
            var dbCaseType = await Get(caseType.Id.Value);
            if (dbCaseType.Code != caseType.Code) {
                throw new ValidationException("Case type code cannot be changed.");
            }

            // Update case type entity
            dbCaseType.Title = caseType.Title;
            dbCaseType.DataSchema = caseType.DataSchema;
            dbCaseType.Layout = caseType.Layout;
            dbCaseType.Translations = caseType.Translations;
            dbCaseType.LayoutTranslations = caseType.LayoutTranslations;
            dbCaseType.Tags = caseType.Tags;

            _dbContext.CaseTypes.Update(dbCaseType);


            // Update related case type roles
            var caseTypeRoles = await _dbContext.RoleCaseTypes.AsQueryable()
                .Where(p => p.CaseTypeId == dbCaseType.Id)
                .ToListAsync();

            // Remove completely the existing roles
            _dbContext.RemoveRange(caseTypeRoles);

            // Insert the new roles for each checkpoint type
            foreach (var checkpointType in caseType.CheckpointTypes!) {
                foreach (var role in (checkpointType.Roles ?? Enumerable.Empty<string>())) {
                    await _dbContext.RoleCaseTypes.AddAsync(new DbRoleCaseType {
                        CaseTypeId = dbCaseType.Id,
                        CheckpointTypeId = checkpointType.Id,
                        RoleName = role
                    });
                }
            }

            await _dbContext.SaveChangesAsync();
            return await GetCaseTypeDetailsById(caseType.Id.Value);
        }

        private async Task<bool> CaseTypeCodeExists(string caseTypeCode) {
            return await _dbContext.CaseTypes.AsQueryable().AnyAsync(c => c.Code == caseTypeCode);
        }

        private async Task<ResultSet<CaseTypePartial>> GetAdminCases() {
            var caseTypes = await _dbContext.CaseTypes
                .AsQueryable()
                    .Select(c => new CaseTypePartial {
                        Id = c.Id,
                        Title = c.Title,
                        Code = c.Code,
                        Tags = c.Tags
                    })
                    .ToListAsync();
            return caseTypes.ToResultSet();
        }
    }
}