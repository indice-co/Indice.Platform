using System.Security.Claims;
using Indice.Events;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Security;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Indice.Features.Cases.Tests;

public class AdminCaseServiceTests : IAsyncLifetime
{
    public AdminCaseServiceTests() {
        var inMemorySettings = new Dictionary<string, string?> {
            ["ConnectionStrings:CasesDb"] = $"Server=(localdb)\\MSSQLLocalDB;Database=Indice.Features.Cases.Test_{Environment.Version.Major}_{Guid.NewGuid()};Trusted_Connection=True;MultipleActiveResultSets=true",
        };
        Microsoft.Extensions.Configuration.IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .AddUserSecrets<MyCaseServiceTests>(optional: true)
            .Build();
        var collection = new ServiceCollection()
            .AddDbContext<CasesDbContext>(builder => builder.UseSqlServer(configuration.GetConnectionString("CasesDb")))
            .Configure<CasesOptions>(options => {

            });
        ServiceProvider = collection.BuildServiceProvider();

        // ensure created and seed here.
    }

    public ServiceProvider ServiceProvider { get; }

    [Fact(Skip = "Not ready yet")]
    public async Task GetCases() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        var options = ServiceProvider.GetRequiredService<IOptions<CasesOptions>>();
        var mockCaseEventService = Substitute.For<IPlatformEventService>();
        var mockCaseAuthorization = Substitute.For<ICaseAuthorizationProvider>();
        var mockAdminCaseMessage = Substitute.For<IAdminCaseMessageService>();

        var adminCaseService = new AdminCaseService(
            dbContext,
            options,
            mockCaseAuthorization,
            mockAdminCaseMessage,
            mockCaseEventService
        );

        var listOptions = new ListOptions<GetCasesListFilter> {
            Page = 1,
            Size = 10,
            Filter = new GetCasesListFilter {
                Data = [(FilterClause)"data.customerId::Contains::(string)667"]
            }
        };
        var result = await adminCaseService.GetCases(Admin().UserToActor(options.Value), listOptions);

        Assert.NotEmpty(result.Items);
    }

    /// <summary>
    /// Regression: when access rules apply, paging (Skip/Take) pushes the query into a subquery.
    /// An explicit join on Checkpoints caused SQL Server to fail with "Invalid column name 'CheckpointTypeId'".
    /// Runs against SQL Server so it catches translation regressions after EF Core/package upgrades.
    /// </summary>
    [Fact]
    public async Task GetCases_WithAccessRules_AndPaging_DoesNotThrow() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        var options = ServiceProvider.GetRequiredService<IOptions<CasesOptions>>();
        var caseAuthorization = Substitute.For<ICaseAuthorizationProvider>();
        caseAuthorization
            .GetCaseMembership(Arg.Any<IQueryable<CasePartial>>(), Arg.Any<UserActor>())
            .Returns(call => Task.FromResult(call.Arg<IQueryable<CasePartial>>()));
        var adminCaseService = new AdminCaseService(
            dbContext,
            options,
            caseAuthorization,
            Substitute.For<IAdminCaseMessageService>(),
            Substitute.For<IPlatformEventService>()
        );
        var listOptions = new ListOptions<GetCasesListFilter> {
            Page = 1,
            Size = 10,
            Filter = new GetCasesListFilter { ShowAll = false }
        };

        var exception = await Record.ExceptionAsync(() => adminCaseService.GetCases(User().UserToActor(options.Value), listOptions));

        Assert.Null(exception);
    }

    [Fact]
    public async Task UpdateRelationKey_MovesOnlyThatCase() {
        var service = CreateService();
        var a = await AddCase("K1");
        var b = await AddCase("K1");
        var c = await AddCase("K1");

        Assert.True(await service.UpdateRelationKey(a.Id, "K2"));

        var moved = await GetCase(a.Id);
        Assert.Equal("K2", moved.RelationKey);
        Assert.Equal("K2", moved.Metadata![CasesCoreConstants.MetadataKeys.ExternalCorrelationKey]);
        Assert.Equal("K1", (await GetCase(b.Id)).RelationKey);
        Assert.Equal("K1", (await GetCase(c.Id)).RelationKey);
    }

    [Fact]
    public async Task UpdateRelationKey_Empty_LeavesTheRelation() {
        var service = CreateService();
        var a = await AddCase("K1");
        var b = await AddCase("K1");

        Assert.True(await service.UpdateRelationKey(a.Id, ""));

        var left = await GetCase(a.Id);
        Assert.Null(left.RelationKey);
        Assert.False(left.Metadata?.ContainsKey(CasesCoreConstants.MetadataKeys.ExternalCorrelationKey) ?? false);
        Assert.Equal("K1", (await GetCase(b.Id)).RelationKey);
    }

    [Fact]
    public async Task AddRelatedCases_MovesCasesOutOfTheirRelation() {
        var service = CreateService();
        var a = await AddCase("K1");
        var d = await AddCase("K2");
        var e = await AddCase("K2");
        var f = await AddCase(null);

        Assert.True(await service.AddRelatedCases(a.Id, [d.Id, f.Id]));

        Assert.Equal("K1", (await GetCase(d.Id)).RelationKey);
        Assert.Equal("K1", (await GetCase(f.Id)).RelationKey);
        Assert.Equal("K2", (await GetCase(e.Id)).RelationKey);
    }

    [Fact]
    public async Task AddRelatedCases_WithoutRelationKey_Throws() {
        var service = CreateService();
        var a = await AddCase(null);
        var b = await AddCase(null);

        await Assert.ThrowsAsync<BusinessException>(() => service.AddRelatedCases(a.Id, [b.Id]));
    }

    [Fact]
    public async Task RemoveRelatedCase_ClearsOnlyThatCase() {
        var service = CreateService();
        var a = await AddCase("K1");
        var b = await AddCase("K1");
        var c = await AddCase("K1");
        var unrelated = await AddCase("K2");

        Assert.True(await service.RemoveRelatedCase(a.Id, b.Id));
        Assert.False(await service.RemoveRelatedCase(a.Id, unrelated.Id));

        Assert.Null((await GetCase(b.Id)).RelationKey);
        Assert.Equal("K1", (await GetCase(a.Id)).RelationKey);
        Assert.Equal("K1", (await GetCase(c.Id)).RelationKey);
        Assert.Equal("K2", (await GetCase(unrelated.Id)).RelationKey);
    }

    [Fact]
    public async Task UpdateGroup_SetsAndClears() {
        var service = CreateService();
        var a = await AddCase(null);

        Assert.True(await service.UpdateGroup(a.Id, " 010 "));
        Assert.Equal("010", (await GetCase(a.Id)).GroupId);

        Assert.True(await service.UpdateGroup(a.Id, ""));
        Assert.Null((await GetCase(a.Id)).GroupId);

        Assert.False(await service.UpdateGroup(Guid.NewGuid(), "010"));
    }

    private AdminCaseService CreateService() => new(
        ServiceProvider.GetRequiredService<CasesDbContext>(),
        ServiceProvider.GetRequiredService<IOptions<CasesOptions>>(),
        Substitute.For<ICaseAuthorizationProvider>(),
        Substitute.For<IAdminCaseMessageService>(),
        Substitute.For<IPlatformEventService>()
    );

    private async Task<DbCase> AddCase(string? relationKey) {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        var @case = new DbCase {
            CaseTypeId = (await dbContext.CaseTypes.FirstAsync()).Id,
            Channel = "web",
            CreatedBy = new AuditMeta { Id = "tester", Name = "tester", Email = "tester@test.gr" },
            RelationKey = relationKey,
            Metadata = relationKey is null ? null : new() { [CasesCoreConstants.MetadataKeys.ExternalCorrelationKey] = relationKey }
        };
        dbContext.Cases.Add(@case);
        await dbContext.SaveChangesAsync();
        return @case;
    }

    /// <summary>Reads the stored case, bypassing the tracked instances.</summary>
    private Task<DbCase> GetCase(Guid id) =>
        ServiceProvider.GetRequiredService<CasesDbContext>().Cases.AsNoTracking().SingleAsync(c => c.Id == id);

    private static ClaimsPrincipal User() {
        var claims = new List<Claim> {
            new Claim(BasicClaimTypes.Scope, CasesCoreConstants.DefaultScopeName),
            new Claim(BasicClaimTypes.Subject, "c0902ebf-ccdf-4621-b940-9b6384745149"),
            new Claim(BasicClaimTypes.Email, "user@test.gr"),
            new Claim(BasicClaimTypes.GivenName, "Test"),
            new Claim(BasicClaimTypes.FamilyName, "User"),
            new Claim(BasicClaimTypes.Role, "CasesManager")
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Basic"));
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

    public async ValueTask InitializeAsync() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>(); 
        if (await dbContext.Database.EnsureCreatedAsync() || !dbContext.Cases.Any()) {
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