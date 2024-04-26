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
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace Indice.Features.Cases.Services;

/// <inheritdoc/>
public class CaseTypeMenuService(CasesDbContext dbContext) : ICaseTypeMenuService
{
    private readonly CasesDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    /// <inheritdoc/>
    public async Task<ResultSet<CaseTypeMenu>> GetMenuItems(ListOptions options) {
        var result = await _dbContext.CaseTypes.AsNoTracking()
                               .Where(x => x.IsMenuItem)
                               .Select(x => new CaseTypeMenu {
                                   Id = x.Id,
                                   Title = x.Title
                               })
                               .ToResultSetAsync(options);

        return result;
    }
}
