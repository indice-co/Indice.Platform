using System.Text.Json.Nodes;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Exceptions;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services;
using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Indice.Features.Cases.Tests;

/// <summary>Runs against SQL Server (LocalDB) so the aggregate queries are checked for translation as well as results.</summary>
public class AdminDashboardServiceTests : IAsyncLifetime
{
    private const string UserId = "3f1d6a52-6c0e-4d3b-9a63-0d5c1b7e2a11";
    private const string ManagerRole = "CasesManager";

    public AdminDashboardServiceTests() {
        var inMemorySettings = new Dictionary<string, string?> {
            ["ConnectionStrings:CasesDb"] = $"Server=(localdb)\\MSSQLLocalDB;Database=Indice.Features.Cases.Test_{Environment.Version.Major}_{Guid.NewGuid()};Trusted_Connection=True;MultipleActiveResultSets=true",
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .AddUserSecrets<AdminDashboardServiceTests>(optional: true)
            .Build();
        ServiceProvider = new ServiceCollection()
            .AddDbContext<CasesDbContext>(builder => builder.UseSqlServer(configuration.GetConnectionString("CasesDb")))
            .Configure<CasesOptions>(options => { })
            .BuildServiceProvider();
    }

    public ServiceProvider ServiceProvider { get; }

    [Fact]
    public async Task GetMetrics_ForAdmin_CountsAllVisibleNonDraftCases() {
        var service = CreateService(PassThroughMembership());

        var metrics = await service.GetMetrics(new UserActor { Id = UserId, IsAdmin = true }, intakeDays: 14, staleAfterDays: 7);

        Assert.Equal(2, metrics.Statuses.Submitted);
        Assert.Equal(2, metrics.Statuses.InProgress);
        Assert.Equal(1, metrics.Statuses.Completed);
        Assert.Equal(0, metrics.Statuses.Deleted);
        Assert.Equal(4, metrics.Statuses.Open);
        Assert.Equal(2, metrics.AssignedToMe);  // submitted 3 days ago + in progress 10 days ago (the completed one is not open)
        Assert.Equal(2, metrics.Unassigned);    // submitted today + in progress 20 days ago
        Assert.Equal(2, metrics.Stale);         // in progress 10 and 20 days ago
        Assert.Equal(7, metrics.StaleAfterDays);
        Assert.Equal(3, metrics.NewThisWeek);   // today, 1 and 3 days ago; the draft is excluded
        Assert.Equal(1, metrics.NewPreviousWeek);
        Assert.Equal(14, metrics.Intake.Count);
        Assert.Equal(DateOnly.FromDateTime(DateTime.Now), metrics.Intake[^1].Date);
        Assert.Equal(1, metrics.Intake[^1].Count);
        Assert.Equal(4, metrics.Intake.Sum(x => x.Count)); // the case created 20 days ago is outside the window
    }

    [Fact]
    public async Task GetMetrics_ForUserWithAccessRules_CountsOnlyAccessibleCases() {
        var service = CreateService(PassThroughMembership());
        var user = new UserActor { Id = UserId, Roles = [ManagerRole] };

        var metrics = await service.GetMetrics(user, intakeDays: 7, staleAfterDays: 7);

        // the role can only access the "submitted" checkpoint type
        Assert.Equal(2, metrics.Statuses.Submitted);
        Assert.Equal(0, metrics.Statuses.InProgress);
        Assert.Equal(0, metrics.Statuses.Completed);
        Assert.Equal(1, metrics.AssignedToMe);
        Assert.Equal(1, metrics.Unassigned);
        Assert.Equal(0, metrics.Stale);
        Assert.Equal(7, metrics.Intake.Count);
        Assert.Equal(2, metrics.NewThisWeek);
    }

    [Fact]
    public async Task GetMetrics_WhenListingIsUnauthorized_ReturnsEmptyMetrics() {
        var membership = Substitute.For<ICaseAuthorizationProvider>();
        membership.GetCaseMembership(Arg.Any<IQueryable<CasePartial>>(), Arg.Any<UserActor>()).ThrowsAsync(new ResourceUnauthorizedException());
        var service = CreateService(membership);

        var metrics = await service.GetMetrics(new UserActor { Id = UserId }, intakeDays: 5, staleAfterDays: 7);

        Assert.Equal(0, metrics.Statuses.Open);
        Assert.Equal(0, metrics.AssignedToMe);
        Assert.Equal(5, metrics.Intake.Count);
        Assert.All(metrics.Intake, day => Assert.Equal(0, day.Count));
    }

    private AdminDashboardService CreateService(ICaseAuthorizationProvider membership) => new(
        ServiceProvider.GetRequiredService<CasesDbContext>(),
        membership,
        ServiceProvider.GetRequiredService<IOptions<CasesOptions>>());

    private static ICaseAuthorizationProvider PassThroughMembership() {
        var membership = Substitute.For<ICaseAuthorizationProvider>();
        membership
            .GetCaseMembership(Arg.Any<IQueryable<CasePartial>>(), Arg.Any<UserActor>())
            .Returns(call => Task.FromResult(call.Arg<IQueryable<CasePartial>>()));
        return membership;
    }

    public async ValueTask InitializeAsync() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        var caseType = new DbCaseType { Id = Guid.NewGuid(), Code = "DASH", Title = "Dashboard", DataSchema = JsonNode.Parse("{}")! };
        var submitted = new DbCheckpointType { Id = Guid.NewGuid(), CaseTypeId = caseType.Id, Code = "Submitted", Status = CaseStatus.Submitted };
        var inProgress = new DbCheckpointType { Id = Guid.NewGuid(), CaseTypeId = caseType.Id, Code = "InProgress", Status = CaseStatus.InProgress };
        var completed = new DbCheckpointType { Id = Guid.NewGuid(), CaseTypeId = caseType.Id, Code = "Completed", Status = CaseStatus.Completed };
        dbContext.CaseTypes.Add(caseType);
        dbContext.CheckpointTypes.AddRange(submitted, inProgress, completed);

        var now = DateTimeOffset.Now;
        var seeds = new (DbCheckpointType Type, double DaysAgo, string? AssignedTo, bool Draft)[] {
            (submitted, 0, null, false),
            (submitted, 3, UserId, false),
            (inProgress, 10, UserId, false),
            (inProgress, 20, null, false),
            (completed, 1, UserId, false),
            (submitted, 0, UserId, true)
        };
        var cases = seeds.Select(seed => new DbCase {
            Id = Guid.NewGuid(),
            CaseTypeId = caseType.Id,
            Channel = CasesCoreConstants.Channels.Agent,
            Draft = seed.Draft,
            CreatedBy = new AuditMeta { Id = "creator", Name = "Creator", Email = "creator@test.gr", When = now.AddDays(-seed.DaysAgo) },
            AssignedTo = seed.AssignedTo is null ? null : new AuditMeta { Id = seed.AssignedTo, Name = "Assignee", Email = "assignee@test.gr", When = now }
        }).ToList();
        dbContext.Cases.AddRange(cases);
        dbContext.CaseAccessRules.Add(new DbCaseAccessRule { MemberRole = ManagerRole, RuleCheckpointTypeId = submitted.Id, AccessLevel = 111 });
        await dbContext.SaveChangesAsync();

        // case <-> current checkpoint reference each other, so the checkpoints go in a second round trip
        for (var i = 0; i < cases.Count; i++) {
            var checkpoint = new DbCheckpoint { CaseId = cases[i].Id, CheckpointTypeId = seeds[i].Type.Id, CreatedBy = cases[i].CreatedBy };
            dbContext.Checkpoints.Add(checkpoint);
            cases[i].CheckpointId = checkpoint.Id;
        }
        await dbContext.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync() {
        var dbContext = ServiceProvider.GetRequiredService<CasesDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await ServiceProvider.DisposeAsync();
    }
}
