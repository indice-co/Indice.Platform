using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Services;

/// <inheritdoc/>
public class CaseTypeMenuService(CasesDbContext dbContext) : ICaseTypeMenuService
{
    private readonly CasesDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    //private readonly ICaseAuthorizationProvider _memberAuthorizationProvider;

    //public CaseTypeMenuService(ICaseAuthorizationProvider memberAuthorizationProvider) {
    //    _memberAuthorizationProvider = memberAuthorizationProvider ?? throw new ArgumentNullException(nameof(memberAuthorizationProvider));
    //}

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
