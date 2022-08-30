using System;
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

        public async Task<ResultSet<CaseTypePartial>> Get(ClaimsPrincipal user) {
            if (user.IsAdmin()) {
                return await GetAdminCases();
            }
            var roleClaims = user.Claims
                .Where(c => c.Type == JwtClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            var caseTypeIds = await _dbContext.RoleCaseTypes
                .AsQueryable()
                .Where(r => roleClaims.Contains(r.RoleName))
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
                throw new Exception("Case type code already exists.");
            }

            DbCaseType newCaseType = new DbCaseType {
                Id = Guid.NewGuid(),
                Code = caseType.Code,
                Title = caseType.Title,
                DataSchema = caseType.DataSchema,
                Layout = caseType.Layout,
                Translations = caseType.Translations,
                LayoutTranslations = caseType.LayoutTranslations
            };

            await _dbContext.CaseTypes.AddAsync(newCaseType);
            await _dbContext.SaveChangesAsync();
        }

        public async Task Delete(Guid caseTypeId) {
            if (caseTypeId == null) {
                throw new Exception("Case Type id not provided.");
            }
            var casesWithCaseType = await _dbContext.Cases.AsQueryable().AnyAsync(x => x.CaseTypeId == caseTypeId);
            if (casesWithCaseType) {
                throw new Exception("Case type cannot be deleted because there are cases with this type.");
            }
            var dbCaseType = await Get(caseTypeId);
            _dbContext.CaseTypes.Remove(dbCaseType);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<CaseTypeDetails> GetCaseTypeDetailsById(Guid id) {
            var dbCaseType = await Get(id);
            var caseType = new CaseTypeDetails {
                Id = id,
                Code = dbCaseType.Code,
                Title = dbCaseType.Title,
                DataSchema = dbCaseType.DataSchema,
                Layout = dbCaseType.Layout,
                Translations = dbCaseType.Translations,
                LayoutTranslations = dbCaseType.LayoutTranslations
            };

            return caseType;
        }

        public async Task<CaseTypeDetails> Update(CaseTypeRequest caseType) {
            if (!caseType.Id.HasValue) {
                throw new Exception("Case type can not be null");
            }
            var dbCaseType = await Get(caseType.Id.Value);
            if (dbCaseType.Code != caseType.Code) {
                throw new Exception("Case type code cannot be changed");
            }
            dbCaseType.Title = caseType.Title;
            dbCaseType.DataSchema = caseType.DataSchema;
            dbCaseType.Layout = caseType.Layout;
            dbCaseType.Translations = caseType.Translations;
            dbCaseType.LayoutTranslations = dbCaseType.LayoutTranslations;

            _dbContext.CaseTypes.Update(dbCaseType);
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
                        Code = c.Code
                    })
                    .ToListAsync();
            return caseTypes.ToResultSet();
        }

    }
}