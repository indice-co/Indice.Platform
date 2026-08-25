using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Microsoft.EntityFrameworkCore;
using Indice.Security;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Services;
using Indice.Features.Cases.Core;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Tests;

public class AccessRulesServiceTest : IAsyncLifetime
{
    public AccessRulesServiceTest() {
        var inMemorySettings = new Dictionary<string, string?> {
            ["ConnectionStrings:CasesDb"] = $"Server=(localdb)\\MSSQLLocalDB;Database=Indice.Features.Cases.Test_{Environment.Version.Major}_{Guid.NewGuid()};Trusted_Connection=True;MultipleActiveResultSets=true",
            //["ConnectionStrings:CasesDb"] = $"Server=(localdb)\\MSSQLLocalDB;Database=ChaniaBank.Cases_uat;Trusted_Connection=True;MultipleActiveResultSets=true",
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .AddUserSecrets<AccessRulesServiceTest>(optional: true)
            .Build();

        var collection = new ServiceCollection()
            .AddDbContext<CasesDbContext>(builder => builder
            .UseLoggerFactory(LoggerFactory.Create(builder => builder.AddDebug()))
            .UseSqlServer(configuration.GetConnectionString("CasesDb")))
            .Configure<CasesOptions>(options => {

            });
        ServiceProvider = collection.BuildServiceProvider();

        // ensure created and seed here.
    }

    public ServiceProvider ServiceProvider { get; }

    private async Task<DbCase?> FetchCaseForTestAsync(CasesDbContext context) {
        return await context.Cases.FirstOrDefaultAsync();
    }

    private static ClaimsPrincipal Admin() {
        var claims = new List<Claim> {
            new Claim(BasicClaimTypes.Scope, CasesCoreConstants.DefaultScopeName),
            new Claim(BasicClaimTypes.Subject, "CE21AF5A-FEDD-4BD6-BAE3-B7473E8A219D"),
            new Claim(BasicClaimTypes.Email, "Case API"),
            new Claim(BasicClaimTypes.GivenName, "Case API"),
            new Claim(BasicClaimTypes.FamilyName, "Case API"),
            new Claim(BasicClaimTypes.Admin, "true")
        };
        var identity = new ClaimsIdentity(claims, "Basic"); // By setting "Basic" we are making the identity "Authenticated" so we can user user.IsAuthenticated() property later in our code
        return new ClaimsPrincipal(identity);
    }

    private static ClaimsPrincipal NonAdmin() {
        var claims = new List<Claim> {
            new Claim(BasicClaimTypes.Scope, CasesCoreConstants.DefaultScopeName),
            new Claim(BasicClaimTypes.Subject, "CE21AF5A-FEDD-4BD6-BAE3-B7473E8A219E"),
            new Claim(BasicClaimTypes.Email, "Case API"),
            new Claim(BasicClaimTypes.GivenName, "Case API"),
            new Claim(BasicClaimTypes.FamilyName, "Case API"),
            new Claim(BasicClaimTypes.Admin, "false")
        };
        var identity = new ClaimsIdentity(claims, "Basic"); // By setting "Basic" we are making the identity "Authenticated" so we can user user.IsAuthenticated() property later in our code
        return new ClaimsPrincipal(identity);
    }



    [Fact]
    public async Task AddAdmin_AccessRule_ForCase() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        var options = ServiceProvider.GetRequiredService<IOptions<CasesOptions>>();
        var caseMembersService = new AccessRuleService(dbContext);
        var @case = await FetchCaseForTestAsync(dbContext);
        await caseMembersService.Create(Admin().UserToActor(options.Value),
            new() {
                AccessLevel = 110,
                MemberUserId = Guid.NewGuid().ToString(),
                RuleCaseId = @case?.Id
            });
        Assert.True(true);
    }


    [Fact]
    public async Task GetCase_AccessRules() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        var options = ServiceProvider.GetRequiredService<IOptions<CasesOptions>>();
        var caseMembersService = new AccessRuleService(dbContext);
        var @case = await FetchCaseForTestAsync(dbContext);
        Assert.NotNull(@case);
        await caseMembersService.Create(Admin().UserToActor(options.Value),
            new() {
                AccessLevel = 0,
                MemberUserId = Guid.NewGuid().ToString(),
                RuleCaseId = @case.Id
            });
        await caseMembersService.Create(Admin().UserToActor(options.Value),
           new() {
               AccessLevel = 1,
               MemberRole = BasicRoleNames.Administrator,
               RuleCaseTypeId = @case.CaseTypeId
           });

        var rules = await caseMembersService.GetListByCase(@case.Id);
        Assert.True(rules.Count == 2);
    }

    [Fact]
    public async Task AddAdmin_Batch() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        var options = ServiceProvider.GetRequiredService<IOptions<CasesOptions>>();
        var caseMembersService = new AccessRuleService(dbContext);
        var @case = await FetchCaseForTestAsync(dbContext);
        Assert.NotNull(@case);
        await caseMembersService.BatchCreate(Admin().UserToActor(options.Value), [
                new () {
                    AccessLevel = 110,
                    MemberUserId = Guid.NewGuid().ToString(),
                    RuleCaseId = @case.Id
                },
                new () {
                    AccessLevel = 110,
                    MemberRole = BasicRoleNames.Administrator,
                    RuleCaseId = @case.Id
                },
                new () {
                    AccessLevel = 110,
                    MemberGroupId = Guid.NewGuid().ToString(),
                    RuleCaseId = @case.Id
                }
            ]);

        Assert.Equal(3, await dbContext.CaseAccessRules.CountAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Update_AccessRule_ForCase() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        var options = ServiceProvider.GetRequiredService<IOptions<CasesOptions>>();
        var caseMembersService = new AccessRuleService(dbContext);
        var @case = await FetchCaseForTestAsync(dbContext);
        await caseMembersService.Create(Admin().UserToActor(options.Value),
            new() {
                AccessLevel = 110,
                MemberUserId = Guid.NewGuid().ToString(),
                RuleCaseId = @case?.Id
            });
        var caseRule = await dbContext.CaseAccessRules.FirstOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken);
        var updatedCase = await caseMembersService.Update(Admin().UserToActor(options.Value), caseRule!.Id, 100);
        Assert.Equal(100, updatedCase.AccessLevel);
    }

    [Fact]
    public async Task AddRuleCaseType_AccessRule_ForCase() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        var options = ServiceProvider.GetRequiredService<IOptions<CasesOptions>>();
        var caseMembersService = new AccessRuleService(dbContext);
        var caseType = await dbContext.CaseTypes.FirstOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken);
        await caseMembersService.Create(Admin().UserToActor(options.Value),
            new() {
                AccessLevel = 110,
                MemberRole = BasicRoleNames.Administrator,
                RuleCaseTypeId = caseType?.Id
            });
        var caseRule = await dbContext.CaseAccessRules.FirstOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken);
        var updatedRule = await caseMembersService.Update(Admin().UserToActor(options.Value), caseRule!.Id, 100);
        Assert.Equal(100, updatedRule.AccessLevel);
    }

    [Fact]
    public async Task UpdateRuleCaseType_FromAdmin() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        var options = ServiceProvider.GetRequiredService<IOptions<CasesOptions>>();
        var caseMembersService = new AccessRuleService(dbContext);
        var caseType = await dbContext.CaseTypes.FirstOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken);
        await caseMembersService.Create(Admin().UserToActor(options.Value),
            new() {
                AccessLevel = 110,
                MemberRole = BasicRoleNames.Administrator,
                RuleCaseTypeId = caseType?.Id
            });
        var caseRule = await dbContext.CaseAccessRules.FirstOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken);
        var updatedRule = await caseMembersService.Update(Admin().UserToActor(options.Value), caseRule!.Id, 100);
        Assert.Equal(100, updatedRule.AccessLevel);
    }

    [Fact]
    public async Task Exception_UpdateRuleCaseType_FromNonAdmin() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        var options = ServiceProvider.GetRequiredService<IOptions<CasesOptions>>();
        var caseMembersService = new AccessRuleService(dbContext);
        var caseType = await dbContext.CaseTypes.FirstOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken);
        await caseMembersService.Create(Admin().UserToActor(options.Value),
            new() {
                AccessLevel = 110,
                MemberRole = BasicRoleNames.Administrator,
                RuleCaseTypeId = caseType?.Id
            });
        var caseRule = await dbContext.CaseAccessRules.FirstOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken);
        try {
            _ = await caseMembersService.Update(NonAdmin().UserToActor(options.Value), caseRule!.Id, 100);
        } catch (UnauthorizedAccessException) {
            Assert.True(true);
            return;
        }
        Assert.True(false);
    }


    [Fact]
    public async Task Delete_AccessRule_ForCase() {

        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        var options = ServiceProvider.GetRequiredService<IOptions<CasesOptions>>();
        var caseMembersService = new AccessRuleService(dbContext);
        var @case = await FetchCaseForTestAsync(dbContext);

        await caseMembersService.Create(Admin().UserToActor(options.Value),
            new() {
                AccessLevel = 110,
                MemberUserId = Guid.NewGuid().ToString(),
                RuleCaseId = @case?.Id
            });

        var rule = await dbContext.CaseAccessRules.FirstOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken);
        await caseMembersService.Delete(Admin().UserToActor(options.Value), rule!.Id);

        Assert.True(true);
    }

    [Fact]
    public async Task Exception_AddAdmin_AccessRule_ForNonAdminUser() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        var options = ServiceProvider.GetRequiredService<IOptions<CasesOptions>>();
        var caseMembersService = new AccessRuleService(dbContext);
        var @case = await FetchCaseForTestAsync(dbContext);
        Assert.NotNull(@case);
        try {
            await caseMembersService.Create(NonAdmin().UserToActor(options.Value),
                new() {
                    AccessLevel = 110,
                    MemberUserId = Guid.NewGuid().ToString(),
                    RuleCaseId = @case.Id
                });
        } catch (UnauthorizedAccessException) {
            Assert.True(true);
            return;
        }
        Assert.True(false);
    }


    [Fact]
    public async Task ReplaceUser_AccessRules() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        var options = ServiceProvider.GetRequiredService<IOptions<CasesOptions>>();
        var caseMembersService = new AccessRuleService(dbContext);
        var @case = await FetchCaseForTestAsync(dbContext);
        Assert.NotNull(@case);
        var initialUser = Guid.NewGuid().ToString();
        var replacementUser = Guid.NewGuid().ToString();
        await caseMembersService.Create(Admin().UserToActor(options.Value),
            new() {
                AccessLevel = 0,
                MemberUserId = initialUser,
                RuleCaseId = @case.Id
            });
        await caseMembersService.Create(Admin().UserToActor(options.Value),
           new() {
               AccessLevel = 1,
               MemberRole = BasicRoleNames.Administrator,
               RuleCaseTypeId = @case.CaseTypeId
           });

        var result = await caseMembersService.ReplaceUser(Admin().UserToActor(options.Value), @case.Id, initialUser, replacementUser);
        Assert.True(result);
        var rules = await caseMembersService.GetListByCase(@case.Id);
        Assert.True(rules.Exists(x => x.MemberUserId == replacementUser));
    }

    [Fact]
    public async Task ReplaceUser_ThatDoesNotExist_AccessRules() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        var options = ServiceProvider.GetRequiredService<IOptions<CasesOptions>>();
        var caseMembersService = new AccessRuleService(dbContext);
        var @case = await FetchCaseForTestAsync(dbContext);
        Assert.NotNull(@case);
        var initialUser = Guid.NewGuid().ToString();
        var replacementUser = Guid.NewGuid().ToString();
        await caseMembersService.Create(Admin().UserToActor(options.Value),
            new() {
                AccessLevel = 0,
                MemberUserId = initialUser,
                RuleCaseId = @case.Id
            });
        await caseMembersService.Create(Admin().UserToActor(options.Value),
           new() {
               AccessLevel = 1,
               MemberRole = BasicRoleNames.Administrator,
               RuleCaseTypeId = @case.CaseTypeId
           });

        var result = await caseMembersService.ReplaceUser(Admin().UserToActor(options.Value), @case.Id, Guid.NewGuid().ToString(), replacementUser);
        Assert.False(result);
        var rules = await caseMembersService.GetListByCase(@case.Id);
        Assert.True(rules.Exists(x => x.MemberUserId == initialUser));
    }


    public async ValueTask InitializeAsync() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        if (await dbContext.Database.EnsureCreatedAsync() || !await dbContext.Cases.AnyAsync()) {
            // seed here.
            await dbContext.SeedAsync();
        }
    }

    public async ValueTask DisposeAsync() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await ServiceProvider.DisposeAsync();
    }


}