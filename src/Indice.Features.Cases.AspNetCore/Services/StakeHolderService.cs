using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq.Expressions;
using System.Security.Claims;
using DotLiquid.Tags;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Requests;
using Indice.Features.Cases.Models.Responses;
using Indice.Security;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Services;

internal class StakeHolderService : IStakeHolderService
{
    private readonly CasesDbContext _dbContext;

    public StakeHolderService(CasesDbContext dbContext) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task Add(StakeHolderRequest request) {
        if (request.CaseId == Guid.Empty) {
            throw new ValidationException("Case id not provided.");
        }
        if (string.IsNullOrEmpty(request.StakeHolderId)) {
            throw new ValidationException("StakeHolder not provided.");
        }
        var stakeHolder = await _dbContext.StakeHolders.FirstOrDefaultAsync(x => x.CaseId == request.CaseId && x.StakeHolderId == request.StakeHolderId && x.Type == request.Type);
        if (stakeHolder != null) {
            throw new ValidationException("A record already exists.");
        }
        var newStakeHolder = new DbStakeHolder {
            Accesslevel = request.Accesslevel,
            StakeHolderId = request.StakeHolderId,
            CaseId = request.CaseId,
            Type = request.Type,
        };
        await _dbContext.StakeHolders.AddAsync(newStakeHolder);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAccessLevel(StakeHolderRequest request) {
        if (request.CaseId == Guid.Empty) {
            throw new ValidationException("Case id not provided.");
        }
        if (string.IsNullOrEmpty(request.StakeHolderId)) {
            throw new ValidationException("StakeHolder not provided.");
        }
        var stakeHolder = await _dbContext.StakeHolders.FirstOrDefaultAsync(x => x.CaseId == request.CaseId && x.StakeHolderId == request.StakeHolderId && x.Type == request.Type);
        if (stakeHolder == null) {
            throw new ValidationException("No record was found for the provided data.");
        }

        stakeHolder.Accesslevel = request.Accesslevel;
        await _dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<StakeHolder>> Get(Guid caseId) {
        if (caseId == Guid.Empty) {
            throw new ValidationException("Case id not provided.");
        }

        return await _dbContext.StakeHolders
            .AsNoTracking()
            .Where(x => x.CaseId == caseId)
            .Select(stakeHolder =>
                new StakeHolder {
                    CaseId = stakeHolder.CaseId,
                    Accesslevel = stakeHolder.Accesslevel,
                    StakeHolderId = stakeHolder.StakeHolderId,
                    Type = stakeHolder.Type
                })
            .ToListAsync();
    }

    public async Task Delete(Guid caseId, string stakeHolderid, byte type) {
        if (caseId == Guid.Empty) {
            throw new ValidationException("Case id not provided.");
        }
        if (string.IsNullOrEmpty(stakeHolderid)) {
            throw new ValidationException("StakeHolder not provided.");
        }
        var stakeHolder = await _dbContext.StakeHolders.FirstOrDefaultAsync(x => x.CaseId == caseId && x.StakeHolderId == stakeHolderid && x.Type == type);
        if (stakeHolder == null) {
            throw new ValidationException("No record was found for the provided input.");
        }
        _dbContext.StakeHolders.Remove(stakeHolder);
        await _dbContext.SaveChangesAsync();
    }
}
