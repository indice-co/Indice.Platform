using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Services;

public class MenuItemService : IMenuItemService
{
    private readonly CasesDbContext _dbContext;

    public MenuItemService(CasesDbContext dbContext) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IEnumerable<CaseType>> GetMenuItems() {
        var dbCaseTypes = _dbContext.CaseTypes.Where(x => x.IsMenuItem).ToList();

        var caseTypeRoles = _dbContext.Members.AsQueryable()
    .Where(p => dbCaseTypes.Select(x => x.Id).Contains(p.CaseTypeId));

        var caseTypes = dbCaseTypes.Select(x => new CaseType() {
            CanCreateRoles = x.CanCreateRoles,
            Code = x.Code,
            Config = x.Config,
            DataSchema = x.DataSchema,
            Description = x.Description,
            Id = x.Id,
            Layout = x.Layout,
            LayoutTranslations = x.LayoutTranslations,
            Order = x.Order,
            Tags = x.Tags,
            Title = x.Title,
            Translations = x.Translations,
            CheckpointTypes = x.CheckpointTypes?.Select(checkpointType => new CheckpointTypeDetails {
                Id = checkpointType.Id,
                Code = checkpointType.Code,
                Description = checkpointType.Description,
                Private = checkpointType.Private,
                Status = checkpointType.Status,
                Roles = caseTypeRoles.Where(y => y.CaseTypeId == x.Id)
                    .Where(roleCaseType => roleCaseType.CheckpointTypeId == checkpointType.Id)
                    .Select(roleCaseType => roleCaseType.RoleName)
            })
        });

        return caseTypes;
    }
}
