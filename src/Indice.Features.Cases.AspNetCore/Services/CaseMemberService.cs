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

internal class CaseMemberService : ICaseMemberService
{
    private readonly CasesDbContext _dbContext;

    public CaseMemberService(CasesDbContext dbContext) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task Add(CaseMemberRequest request) {
        if (request.CaseId == Guid.Empty) {
            throw new ValidationException("Case id not provided.");
        }
        if (string.IsNullOrEmpty(request.MemberId)) {
            throw new ValidationException("MemberId not provided.");
        }
        //var caseMember = await _dbContext.CaseMembers.FirstOrDefaultAsync(x => x.RuleCaseId == request.CaseId && x.MemberUserId == request.MemberId && x.Type == request.Type);
        ////if (caseMember != null) {
        ////    throw new ValidationException("A record already exists.");
        ////}
        ////var newCaseMember = new DbCaseMember {
        ////    Accesslevel = request.Accesslevel,
        ////    MemberId = request.MemberId,
        ////    CaseId = request.CaseId,
        ////    Type = request.Type,
        ////};
        //await _dbContext.CaseMembers.AddAsync(newCaseMember);
        //await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAccessLevel(CaseMemberRequest request) {
        if (request.CaseId == Guid.Empty) {
            throw new ValidationException("Case id not provided.");
        }
        if (string.IsNullOrEmpty(request.MemberId)) {
            throw new ValidationException("CaseMember not provided.");
        }
        //var caseMember = await _dbContext.CaseMembers.FirstOrDefaultAsync(x => x.CaseId == request.CaseId && x.MemberId == request.MemberId && x.Type == request.Type)
        //                        ?? throw new ValidationException("No record was found for the provided data.");
        //caseMember.Accesslevel = request.Accesslevel;
        //await _dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<CaseMember>> Get(Guid caseId) {
        if (caseId == Guid.Empty) {
            throw new ValidationException("Case id not provided.");
        }

        return await _dbContext.CaseMembers
            .AsNoTracking()
            //.Where(x => x.CaseId == caseId)
            .Select(caseMember =>
                new CaseMember {
                    CaseId = caseMember.RuleCaseId.Value,
                    Accesslevel = caseMember.AccessLevel,
                    MemberId = caseMember.MemberUserId,
                    
                })
            .ToListAsync();
    }

    public async Task Delete(CaseMemberDeleteRequest request) {
        if (request is null) {
            throw new ValidationException("Request is empty.");
        }
        if (request.CaseId == Guid.Empty) {
            throw new ValidationException("Case id not provided.");
        }
        if (string.IsNullOrEmpty(request.CaseMemberId)) {
            throw new ValidationException("CaseMember not provided.");
        }
        //var caseMember = await _dbContext.CaseMembers.FirstOrDefaultAsync(x => x.CaseId == request.CaseId && x.MemberId == request.CaseMemberId && x.Type == request.Type) 
        //                            ?? throw new ValidationException("No record was found for the provided input.");
        //_dbContext.CaseMembers.Remove(caseMember);
        //await _dbContext.SaveChangesAsync();
    }
}
