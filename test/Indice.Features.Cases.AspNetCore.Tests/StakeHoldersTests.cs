using System.Linq.Expressions;
using System.Security.Claims;
using Elsa.Models;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Services;
using Indice.Security;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Indice.Features.Cases.Tests;

public class CaseMembersTests : IAsyncDisposable
{
    public CaseMembersTests() {
        var inMemorySettings = new Dictionary<string, string> {
            ["ConnectionStrings:CasesDb"] = $"Server=(localdb)\\MSSQLLocalDB;Database=Indice.Features.Cases.Test_{Environment.Version.Major}_{Guid.NewGuid()};Trusted_Connection=True;MultipleActiveResultSets=true",
        };
        Microsoft.Extensions.Configuration.IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .AddUserSecrets<MyCaseServiceTests>(optional: true)
            .Build();
        var collection = new ServiceCollection()
            .AddDbContext<CasesDbContext>(builder => builder.UseSqlServer(configuration.GetConnectionString("CasesDb")));
        ServiceProvider = collection.BuildServiceProvider();

        // ensure created and seed here.
    }

    public ServiceProvider ServiceProvider { get; }

    [Fact]
    public async Task GetCaseMembers() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        if (await dbContext.Database.EnsureCreatedAsync() || !await dbContext.Cases.AnyAsync()) {
            // seed here.
            await dbContext.SeedAsync();
        }

        var caseMembersService = new CaseMemberService(dbContext);
        var @case = await FetchCaseForTestAsync(dbContext);
        var result = await caseMembersService.Get(@case.Id);

        Assert.NotEmpty(result);
    }


    [Fact]
    public async Task AddCaseMember() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        if (await dbContext.Database.EnsureCreatedAsync() || !await dbContext.Cases.AnyAsync()) {
            // seed here.
            await dbContext.SeedAsync();
        }

        var caseMembersService = new CaseMemberService(dbContext);
        var @case = await FetchCaseForTestAsync(dbContext);
        var id = Guid.NewGuid().ToString();
        await caseMembersService.Add(new Models.Requests.CaseMemberRequest(@case.Id, id, 1, 100));
        var result = await caseMembersService.Get(@case.Id);
        Assert.NotNull(result.FirstOrDefault(x => x.MemberId == id));
    }

    [Fact]
    public async Task RemoveCaseMember() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        if (await dbContext.Database.EnsureCreatedAsync() || !await dbContext.Cases.AnyAsync()) {
            // seed here.
            await dbContext.SeedAsync();
        }

        var @case = await FetchCaseForTestAsync(dbContext);
        var caseMembersService = new CaseMemberService(dbContext);
        //add a new comment
        var id = Guid.NewGuid().ToString();
        await caseMembersService.Add(new Models.Requests.CaseMemberRequest(@case.Id, id, 1, 100));
        var caseMembers = await caseMembersService.Get(@case.Id);
        Assert.NotNull(caseMembers.FirstOrDefault(x => x.MemberId == id));
        var caseMemberToDelete = new Models.Requests.CaseMemberDeleteRequest(@case.Id, id, 1);
        await caseMembersService.Delete(caseMemberToDelete);
        caseMembers = await caseMembersService.Get(@case.Id);
        Assert.Null(caseMembers.FirstOrDefault(x => x.MemberId == id));
    }

    [Fact]
    public async Task UpdateCaseMemberAccessLevel() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        if (await dbContext.Database.EnsureCreatedAsync() || !await dbContext.Cases.AnyAsync()) {
            // seed here.
            await dbContext.SeedAsync();
        }

        var @case = await FetchCaseForTestAsync(dbContext);
        var caseMembersService = new CaseMemberService(dbContext);

        var dbCaseMember = @case.CaseMembers.First();
        var newAccessLevel = dbCaseMember.Accesslevel * 10;
        await caseMembersService.UpdateAccessLevel(new Models.Requests.CaseMemberRequest(@case.Id, dbCaseMember.MemberId, dbCaseMember.Type, newAccessLevel));
        var caseMembers = await caseMembersService.Get(@case.Id);
        var updatedCaseMember = caseMembers.FirstOrDefault(x => x.MemberId == dbCaseMember.MemberId);
        Assert.Equal(newAccessLevel, updatedCaseMember.Accesslevel);
    }

    [Fact]
    public async Task QueryCaseMember() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        if (await dbContext.Database.EnsureCreatedAsync() || !await dbContext.Cases.AnyAsync()) {
            // seed here.
            await dbContext.SeedAsync();
        }

        var caseMembersService = new CaseMemberService(dbContext);
        var @case = await FetchCaseForTestAsync(dbContext);
        var caseMemberId = Guid.NewGuid().ToString();
        await caseMembersService.Add(new Models.Requests.CaseMemberRequest(@case.Id, caseMemberId, 1, 100));
        var listOptions = new ListOptions<GetCasesListFilter> {
            Page = 1,
            Size = 10,
            Filter = new GetCasesListFilter {
                CaseMembers = new List<CaseMemberFilter> { new CaseMemberFilter(caseMemberId, 1) }
            }
        };


        var expressions = listOptions.Filter.CaseMembers
           .Select(f => (Expression<Func<DbCase, bool>>)(c => c.CaseMembers.Any(sh => sh.MemberId == f.Id && sh.Type == f.Type)))
           .ToList();
        // Aggregate the expressions with OR in SQL
        var aggregatedExpressions = expressions.Aggregate((expression, next) => {
            var orExp = Expression.OrElse(expression.Body, Expression.Invoke(next, expression.Parameters));
            return Expression.Lambda<Func<DbCase, bool>>(orExp, expression.Parameters);
        });
        var result = await dbContext.Cases
            .AsNoTracking()
           .Where(aggregatedExpressions)
           .ToListAsync(); 
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task NotFoundCaseMember() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        if (await dbContext.Database.EnsureCreatedAsync() || !await dbContext.Cases.AnyAsync()) {
            // seed here.
            await dbContext.SeedAsync();
        }

        var caseMembersService = new CaseMemberService(dbContext);
        var @case = await FetchCaseForTestAsync(dbContext);
        var caseMemberId = Guid.NewGuid().ToString();
        await caseMembersService.Add(new Models.Requests.CaseMemberRequest(@case.Id, caseMemberId, 1, 100));
        var listOptions = new ListOptions<GetCasesListFilter> {
            Page = 1,
            Size = 10,
            Filter = new GetCasesListFilter {
                CaseMembers = new List<CaseMemberFilter> { new CaseMemberFilter(caseMemberId, 2) }
            }
        };


        var expressions = listOptions.Filter.CaseMembers
           .Select(f => (Expression<Func<DbCase, bool>>)(c => c.CaseMembers.Any(sh => sh.MemberId == f.Id && sh.Type == f.Type)))
           .ToList();
        // Aggregate the expressions with OR in SQL
        var aggregatedExpressions = expressions.Aggregate((expression, next) => {
            var orExp = Expression.OrElse(expression.Body, Expression.Invoke(next, expression.Parameters));
            return Expression.Lambda<Func<DbCase, bool>>(orExp, expression.Parameters);
        });
        var result = await dbContext.Cases
            .AsNoTracking()
           .Where(aggregatedExpressions)
           .ToListAsync();
        Assert.Empty(result);
    }


    private async Task<DbCase> FetchCaseForTestAsync(CasesDbContext context) {

        return await context.Cases.Where(x => x.CaseMembers.Any()).FirstAsync();
    }


    private static ClaimsPrincipal Admin() {
        var claims = new List<Claim> {
            new Claim(BasicClaimTypes.Scope, CasesApiConstants.Scope),
            new Claim(BasicClaimTypes.Subject, "CE21AF5A-FEDD-4BD6-BAE3-B7473E8A219D"),
            new Claim(BasicClaimTypes.Email, "Case API"),
            new Claim(BasicClaimTypes.GivenName, "Case API"),
            new Claim(BasicClaimTypes.FamilyName, "Case API"),
            new Claim(BasicClaimTypes.Admin, "true")
        };
        var identity = new ClaimsIdentity(claims, "Basic"); // By setting "Basic" we are making the identity "Authenticated" so we can user user.IsAuthenticated() property later in our code
        return new ClaimsPrincipal(identity);
    }

    public async ValueTask DisposeAsync() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await ServiceProvider.DisposeAsync();
    }
}