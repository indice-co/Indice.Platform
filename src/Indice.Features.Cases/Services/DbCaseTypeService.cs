using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Services
{
    internal class DbCaseTypeService : ICaseTypeService
    {
        private readonly CasesDbContext _dbContext;

        public DbCaseTypeService(CasesDbContext dbContext) {
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

        public async Task<List<CaseType>> Get(ClaimsPrincipal user) {
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
                .Select(c => new CaseType {
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

            return caseTypes;
        }
    }
}