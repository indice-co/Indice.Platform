using System.Security.Claims;
using Indice.Events;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Localization;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Services;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Security;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Tests;

public class MyCaseServiceTests : IDisposable
{
    public MyCaseServiceTests() {
        var inMemorySettings = new Dictionary<string, string> {
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


    [Fact(Skip = "Not ready yet. Fix the seed method")]
    public async Task CaseOrderBy_DoesNot_Throw_Test() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        var options = ServiceProvider.GetRequiredService<IOptions<CasesOptions>>();
        if (await dbContext.Database.EnsureCreatedAsync() || !dbContext.Cases.Any()) {
            // seed here.
            await dbContext.SeedAsync();
        }
        var a = await dbContext.Cases.ToListAsync();
        var mockCaseTypeService = new Mock<ICaseTypeService>();
        var mockCaseEventService = new Mock<IPlatformEventService>();
        var mockMyCaseMessageService = new Mock<IMyCaseMessageService>();
        var mockJsonTranslationService = new Mock<IJsonTranslationService>();
        var mockResourceService = new CaseSharedResourceService(new Mock<IStringLocalizerFactory>().Object);
        
        var myCaseService = new MyCaseService(dbContext,
            options,
            mockCaseTypeService.Object,
            mockCaseEventService.Object,
            mockMyCaseMessageService.Object,
            mockJsonTranslationService.Object,
            mockResourceService);
        var listOptions = new ListOptions<GetMyCasesListFilter>() { };
        //listOptions.AddSort(new SortByClause("checkpointcontainsDownloaded", "DESC"));
        listOptions.AddSort(new SortByClause("Created", "DESC"));
        _ = await myCaseService.GetCases(User(), listOptions);
    }

    private static ClaimsPrincipal User() {
        var claims = new List<Claim> {
                new Claim(BasicClaimTypes.Scope, CasesApiConstants.Scope),
                new Claim(BasicClaimTypes.Subject, "ab9769f1-d532-4b7d-9922-3da003157ebd"),
                new Claim(BasicClaimTypes.Email, "Case API"),
                new Claim(BasicClaimTypes.GivenName, "Case API"),
                new Claim(BasicClaimTypes.FamilyName, "Case API"),
            };
        var identity = new ClaimsIdentity(claims, "Basic"); // By setting "Basic" we are making the identity "Authenticated" so we can user user.IsAuthenticated() property later in our code
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public async Task MyCaseService_FilterData_Pagination() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        var myOptions = ServiceProvider.GetRequiredService<IOptions<CasesOptions>>();
        if (await dbContext.Database.EnsureCreatedAsync() || !dbContext.Cases.Any()) {
            // seed here.
            await dbContext.SeedAsync();
        }

        var mockCaseTypeService = new Mock<ICaseTypeService>();
        var mockCaseEventService = new Mock<IPlatformEventService>();
        var mockMyCaseMessageService = new Mock<IMyCaseMessageService>();
        var mockJsonTranslationService = new Mock<IJsonTranslationService>();
        var mockResourceService = new CaseSharedResourceService(new Mock<IStringLocalizerFactory>().Object);

        var myCaseService = new MyCaseService(dbContext,
            myOptions,
            mockCaseTypeService.Object,
            mockCaseEventService.Object,
            mockMyCaseMessageService.Object,
            mockJsonTranslationService.Object,
            mockResourceService);
        var listOptions = new ListOptions<GetMyCasesListFilter>() {
            Page = 1,
            Size = 1,
            Filter = new GetMyCasesListFilter() {
                Data = [FilterClause.Parse("data.customerId::Contains::(string)667")]
            }
        };

        var result = await myCaseService.GetCases(User(), listOptions);
        Assert.NotEmpty(result.Items);
    }

    public void Dispose() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        dbContext.Database.EnsureDeleted();
        ServiceProvider.Dispose();
    }
}