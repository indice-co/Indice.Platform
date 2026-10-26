using System.Text.Json.Nodes;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Indice.Features.Cases.Tests;

/// <summary>Runs against SQL Server (LocalDB) so the channel filter is checked for translation as well as results.</summary>
public class AdminReportServiceTests : IAsyncLifetime
{
    private readonly DbCaseType _caseType = new() { Id = Guid.NewGuid(), Code = "REPORT", Title = "Report", DataSchema = JsonNode.Parse("{}")! };

    public AdminReportServiceTests() {
        var inMemorySettings = new Dictionary<string, string?> {
            ["ConnectionStrings:CasesDb"] = $"Server=(localdb)\\MSSQLLocalDB;Database=Indice.Features.Cases.Test_{Environment.Version.Major}_{Guid.NewGuid()};Trusted_Connection=True;MultipleActiveResultSets=true",
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .AddUserSecrets<AdminReportServiceTests>(optional: true)
            .Build();
        ServiceProvider = new ServiceCollection()
            .AddDbContext<CasesDbContext>(builder => builder.UseSqlServer(configuration.GetConnectionString("CasesDb")))
            .BuildServiceProvider();
    }

    public ServiceProvider ServiceProvider { get; }

    [Theory]
    [InlineData(ReportTag.GroupedByStatus, null, 6)]                                          // every submitted case, whatever its channel
    [InlineData(ReportTag.GroupedByStatus, CasesCoreConstants.Channels.Managed, 3)]           // Managed + legacy Agent
    [InlineData(ReportTag.GroupedByStatus, CasesCoreConstants.Channels.Agent, 3)]             // a legacy name means the same channel
    [InlineData(ReportTag.GroupedByCasetype, CasesCoreConstants.Channels.External, 2)]        // External + legacy Customer
    [InlineData(ReportTag.GroupedByCasetype, "Branch", 1)]                                    // a channel with no legacy name
    [InlineData(ReportTag.AgentGroupedByStatus, null, 3)]                                     // the legacy tags are the Managed/External reports
    [InlineData(ReportTag.CustomerGroupedByCasetype, null, 2)]
    [InlineData(ReportTag.AgentGroupedByStatus, CasesCoreConstants.Channels.External, 3)]       // a legacy tag ignores the channel parameter
    public async Task GenerateReport_CountsTheCasesOfTheChannel(ReportTag reportTag, string? channel, int expected) {
        var service = CreateService();

        var report = await service.GenerateReport(new UserActor { Id = "admin", IsAdmin = true }, reportTag, channel);

        Assert.Equal(expected, report.Sum(x => x.Count));
    }

    [Fact]
    public async Task GenerateReport_ForCaseTypes_LabelsWithTheCaseTypeTitle() {
        var service = CreateService();

        var report = await service.GenerateReport(new UserActor { Id = "admin", IsAdmin = true }, ReportTag.GroupedByCasetype, CasesCoreConstants.Channels.Managed);

        var row = Assert.Single(report);
        Assert.Equal(_caseType.Title, row.Label);
        Assert.Equal(3, row.Count);
    }

    private AdminReportService CreateService() {
        var membership = Substitute.For<ICaseAuthorizationProvider>();
        membership
            .GetCaseMembership(Arg.Any<IQueryable<CasePartial>>(), Arg.Any<UserActor>())
            .Returns(call => Task.FromResult(call.Arg<IQueryable<CasePartial>>()));
        var caseTypes = Substitute.For<ICaseTypeService>();
        caseTypes
            .Get(Arg.Any<UserActor>(), false)
            .Returns(new List<CaseTypePartial> { new() { Id = _caseType.Id, Code = _caseType.Code, Title = _caseType.Title } }.ToResultSet());
        return new AdminReportService(ServiceProvider.GetRequiredService<CasesDbContext>(), membership, caseTypes);
    }

    public async ValueTask InitializeAsync() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        var submitted = new DbCheckpointType { Id = Guid.NewGuid(), CaseTypeId = _caseType.Id, Code = "Submitted", Status = CaseStatus.Submitted };
        dbContext.CaseTypes.Add(_caseType);
        dbContext.CheckpointTypes.Add(submitted);

        var seeds = new (string Channel, bool Draft)[] {
            (CasesCoreConstants.Channels.Managed, false),
            (CasesCoreConstants.Channels.Managed, false),
            (CasesCoreConstants.Channels.Agent, false),
            (CasesCoreConstants.Channels.External, false),
            (CasesCoreConstants.Channels.Customer, false),
            ("Branch", false),
            (CasesCoreConstants.Channels.Managed, true)
        };
        var cases = seeds.Select(seed => new DbCase {
            Id = Guid.NewGuid(),
            CaseTypeId = _caseType.Id,
            Channel = seed.Channel,
            Draft = seed.Draft,
            CreatedBy = new AuditMeta { Id = "creator", Name = "Creator", Email = "creator@test.gr", When = DateTimeOffset.Now }
        }).ToList();
        dbContext.Cases.AddRange(cases);
        await dbContext.SaveChangesAsync();

        // case <-> current checkpoint reference each other, so the checkpoints go in a second round trip
        foreach (var @case in cases) {
            var checkpoint = new DbCheckpoint { CaseId = @case.Id, CheckpointTypeId = submitted.Id, CreatedBy = @case.CreatedBy };
            dbContext.Checkpoints.Add(checkpoint);
            @case.CheckpointId = checkpoint.Id;
        }
        await dbContext.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await ServiceProvider.DisposeAsync();
    }
}
