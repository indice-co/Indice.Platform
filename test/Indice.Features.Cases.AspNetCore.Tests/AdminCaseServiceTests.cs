using System.Security.Claims;
using Indice.Features.Cases.Data;
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

public class AdminCaseServiceTests : IDisposable
{
    public AdminCaseServiceTests() {
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

    [Fact(Skip = "IQueryable throws exception, needs fixing")]
    public async Task GetCases() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        if (await dbContext.Database.EnsureCreatedAsync() || !dbContext.Cases.Any()) {
            // seed here.
            await dbContext.SeedAsync();
        }

        var mockCaseTypeService = Substitute.For<ICaseTypeService>();
        var mockCaseEventService = Substitute.For<ICaseEventService>();
        var mockCaseAuthorization = Substitute.For<ICaseAuthorizationProvider>();
        var mockAdminCaseMessage = Substitute.For<IAdminCaseMessageService>();

        var adminCaseService = new AdminCaseService(
            dbContext,
            new AdminCasesApiOptions(),
            mockCaseAuthorization,
            mockCaseTypeService,
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
        var result = await adminCaseService.GetCases(Admin(), listOptions);

        Assert.NotEmpty(result.Items);
    }

    public void Dispose() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        dbContext.Database.EnsureDeleted();
        ServiceProvider.Dispose();
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
}