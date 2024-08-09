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

public class StakeHoldersTests : IAsyncDisposable
{
    public StakeHoldersTests() {
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
    public async Task GetStakeHolders() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        if (await dbContext.Database.EnsureCreatedAsync() || !await dbContext.Cases.AnyAsync()) {
            // seed here.
            await dbContext.SeedAsync();
        }

        var stakeHoldersService = new StakeHolderService(dbContext);
        var @case = await FetchCaseForTestAsync(dbContext);
        var result = await stakeHoldersService.Get(@case.Id);

        Assert.NotEmpty(result);
    }


    [Fact]
    public async Task AddStakeholder() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        if (await dbContext.Database.EnsureCreatedAsync() || !await dbContext.Cases.AnyAsync()) {
            // seed here.
            await dbContext.SeedAsync();
        }

        var stakeHoldersService = new StakeHolderService(dbContext);
        var @case = await FetchCaseForTestAsync(dbContext);
        var id = Guid.NewGuid().ToString();
        await stakeHoldersService.Add(new Models.Requests.StakeHolderRequest(@case.Id, id, 1, 100));
        var result = await stakeHoldersService.Get(@case.Id);
        Assert.NotNull(result.FirstOrDefault(x => x.StakeHolderId == id));
    }

    [Fact]
    public async Task RemoveStakeHolder() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        if (await dbContext.Database.EnsureCreatedAsync() || !await dbContext.Cases.AnyAsync()) {
            // seed here.
            await dbContext.SeedAsync();
        }

        var @case = await FetchCaseForTestAsync(dbContext);
        var stakeHoldersService = new StakeHolderService(dbContext);
        //add a new comment
        var id = Guid.NewGuid().ToString();
        await stakeHoldersService.Add(new Models.Requests.StakeHolderRequest(@case.Id, id, 1, 100));
        var stakeHolders = await stakeHoldersService.Get(@case.Id);
        Assert.NotNull(stakeHolders.FirstOrDefault(x => x.StakeHolderId == id));
        var stakeHolderToDelete = new Models.Requests.StakeHolderDeleteRequest(@case.Id, id, 1);
        await stakeHoldersService.Delete(stakeHolderToDelete);
        stakeHolders = await stakeHoldersService.Get(@case.Id);
        Assert.Null(stakeHolders.FirstOrDefault(x => x.StakeHolderId == id));
    }

    [Fact]
    public async Task UpdateStakeHolderAccessLevel() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        if (await dbContext.Database.EnsureCreatedAsync() || !await dbContext.Cases.AnyAsync()) {
            // seed here.
            await dbContext.SeedAsync();
        }

        var @case = await FetchCaseForTestAsync(dbContext);
        var stakeHoldersService = new StakeHolderService(dbContext);

        var dbStakeHolder = @case.StakeHolders.First();
        var newAccessLevel = dbStakeHolder.Accesslevel * 10;
        await stakeHoldersService.UpdateAccessLevel(new Models.Requests.StakeHolderRequest(@case.Id, dbStakeHolder.StakeHolderId, dbStakeHolder.Type, newAccessLevel));
        var stakeHolders = await stakeHoldersService.Get(@case.Id);
        var updatedStakeHolder = stakeHolders.FirstOrDefault(x => x.StakeHolderId == dbStakeHolder.StakeHolderId);
        Assert.Equal(newAccessLevel, updatedStakeHolder.Accesslevel);
    }

    [Fact]
    public async Task QueryStakeHolder() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        if (await dbContext.Database.EnsureCreatedAsync() || !await dbContext.Cases.AnyAsync()) {
            // seed here.
            await dbContext.SeedAsync();
        }

        var stakeHoldersService = new StakeHolderService(dbContext);
        var @case = await FetchCaseForTestAsync(dbContext);
        var stakeHolderId = Guid.NewGuid().ToString();
        await stakeHoldersService.Add(new Models.Requests.StakeHolderRequest(@case.Id, stakeHolderId, 1, 100));
        var listOptions = new ListOptions<GetCasesListFilter> {
            Page = 1,
            Size = 10,
            Filter = new GetCasesListFilter {
                StakeHolders = new List<StakeHolderFilter> { new StakeHolderFilter(stakeHolderId, 1) }
            }
        };


        var expressions = listOptions.Filter.StakeHolders
           .Select(f => (Expression<Func<DbCase, bool>>)(c => c.StakeHolders.Any(sh => sh.StakeHolderId == f.Id && sh.Type == f.Type)))
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
    public async Task NotFoundStakeHolder() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        if (await dbContext.Database.EnsureCreatedAsync() || !await dbContext.Cases.AnyAsync()) {
            // seed here.
            await dbContext.SeedAsync();
        }

        var stakeHoldersService = new StakeHolderService(dbContext);
        var @case = await FetchCaseForTestAsync(dbContext);
        var stakeHolderId = Guid.NewGuid().ToString();
        await stakeHoldersService.Add(new Models.Requests.StakeHolderRequest(@case.Id, stakeHolderId, 1, 100));
        var listOptions = new ListOptions<GetCasesListFilter> {
            Page = 1,
            Size = 10,
            Filter = new GetCasesListFilter {
                StakeHolders = new List<StakeHolderFilter> { new StakeHolderFilter(stakeHolderId, 2) }
            }
        };


        var expressions = listOptions.Filter.StakeHolders
           .Select(f => (Expression<Func<DbCase, bool>>)(c => c.StakeHolders.Any(sh => sh.StakeHolderId == f.Id && sh.Type == f.Type)))
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

        return await context.Cases.Where(x => x.StakeHolders.Any()).FirstAsync();
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