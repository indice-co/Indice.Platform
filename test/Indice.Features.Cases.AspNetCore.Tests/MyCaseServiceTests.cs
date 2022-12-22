using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Resources;
using Indice.Features.Cases.Services;
using Indice.Security;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Cases.Tests
{
    public class MyCaseServiceTests : IDisposable
    {
        public MyCaseServiceTests() {
            var inMemorySettings = new Dictionary<string, string> {
                ["ConnectionStrings:CasesDb"] = "Server=(localdb)\\MSSQLLocalDB;Database=Indice.Features.Cases.Test;Trusted_Connection=True;MultipleActiveResultSets=true",
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


        [Fact(Skip = "Not ready yet. Fix the seed method")]
        public async Task CaseOrderBy_DoesNot_Throw_Test() {
            var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
            if (await dbContext.Database.EnsureCreatedAsync() || !dbContext.Cases.Any()) {
                // seed here.
                await dbContext.SeedAsync();
            }
            var a = await dbContext.Cases.ToListAsync();
            var mockCaseTypeService = new Mock<ICaseTypeService>();
            var mockCaseEventService = new Mock<ICaseEventService>();
            var mockMyCaseMessageService = new Mock<IMyCaseMessageService>();
            var mockJsonTranslationService = new Mock<IJsonTranslationService>();
            var mockResourceService = new CaseSharedResourceService(new Mock<IStringLocalizerFactory>().Object);

            var myCaseService = new MyCaseService(dbContext,
                mockCaseTypeService.Object,
                mockCaseEventService.Object,
                mockMyCaseMessageService.Object,
                mockJsonTranslationService.Object,
                mockResourceService);
            var options = new ListOptions<GetMyCasesListFilter>() { };
            //options.AddSort(new SortByClause("checkpointcontainsDownloaded", "DESC"));
            options.AddSort(new SortByClause("Created", "DESC"));
            var result = await myCaseService.GetCases(User(), options);
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
        public void Dispose() {
            var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
            dbContext.Database.EnsureDeleted();
            ServiceProvider.Dispose();
        }
    }

}
