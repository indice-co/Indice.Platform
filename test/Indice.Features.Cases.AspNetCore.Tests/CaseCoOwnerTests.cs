using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentValidation;
using Indice.Events;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Events;
using Indice.Features.Cases.Core.Localization;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Services;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Features.Cases.Core.Services.CaseMessageService;
using Indice.Features.Cases.Server;
using Indice.Features.Cases.Server.Authorization;
using Indice.Features.Cases.Server.Endpoints.Validators;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Indice.Features.Cases.Tests;

public class CaseCoOwnerTests : IAsyncDisposable
{
    private const string Owner = "owner";
    private const string Reader = "reader";
    private const string Contributor = "contributor";
    private const string FullCoOwner = "full";
    private const string Stranger = "stranger";
    private const string Administrator = "administrator";
    private const string Manager = "manager";
    private static readonly UserActor BackOfficeUser = new() { Id = Administrator, Name = "Back Office", Email = "back-office@example.test" };
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly Guid _draftId = Guid.NewGuid();
    private readonly Guid _secondDraftId = Guid.NewGuid();
    private readonly Guid _submittedId = Guid.NewGuid();
    private readonly Guid _completedId = Guid.NewGuid();
    private DbContextOptions<CasesDbContext>? _sqlServerOptions;

    [Fact]
    public void Model_maps_the_co_owners_to_their_own_table_with_one_cascading_relationship() {
        using var db = new CasesDbContext(new DbContextOptionsBuilder<CasesDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        var entity = db.Model.FindEntityType(typeof(DbCoownsCase))!;
        Assert.Equal("CaseCoOwner", entity.GetTableName());
        var foreignKey = Assert.Single(entity.GetForeignKeys());
        Assert.Equal(typeof(DbCase), foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(nameof(DbCase.CoOwners), foreignKey.PrincipalToDependent!.Name);
        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
    }

    [Fact]
    public async Task Assign_adds_then_updates_a_co_owner_and_enforces_the_limit() {
        await using var db = CreateInMemoryContext();
        await Seed(db);
        var events = Substitute.For<IPlatformEventService>();
        var permissions = Substitute.For<ICaseContactPermissionService>();
        var service = CreateAdminService(db, events, permissions, coOwnerLimit: 1);

        var added = await service.AssignCoOwner(BackOfficeUser, _submittedId, new ContactMeta {
            UserId = "co-1", Reference = "REF-1", FirstName = "Maria", LastName = "Doe", Tin = "123456789", Permission = ContactPermission.Reader
        });

        Assert.True(added);
        var row = await db.CaseCoOwners.AsNoTracking().SingleAsync(x => x.CaseId == _submittedId, TestContext.Current.CancellationToken);
        Assert.Equal("co-1", row.CoOwner.UserId);
        Assert.Equal("REF-1", row.CoOwner.Reference);
        Assert.Equal("123456789", row.CoOwner.Tin);
        Assert.Equal(ContactPermission.Reader, row.CoOwner.Permission);
        Assert.Equal(Administrator, row.CoOwner.AddedBy.Id);
        Assert.NotNull(row.CoOwner.AddedBy.When);
        await events.Received(1).Publish(Arg.Is<IPlatformEvent>(e => IsAddedEvent(e, _submittedId, "co-1", ContactPermission.Reader)));
        await permissions.Received(1).Invalidate("co-1", _submittedId);

        // the same user again is an update: it does not count against the limit and publishes nothing
        Assert.True(await service.AssignCoOwner(BackOfficeUser, _submittedId, new ContactMeta { UserId = "co-1", FirstName = "Maria", LastName = "Smith", Permission = ContactPermission.Contributor }));
        row = await db.CaseCoOwners.AsNoTracking().SingleAsync(x => x.CaseId == _submittedId, TestContext.Current.CancellationToken);
        Assert.Equal(ContactPermission.Contributor, row.CoOwner.Permission);
        Assert.Equal("Smith", row.CoOwner.LastName);
        await events.Received(1).Publish(Arg.Any<CaseCoOwnerAddedEvent>());
        await permissions.Received(2).Invalidate("co-1", _submittedId);

        // a second co-owner exceeds the limit of one
        await Assert.ThrowsAsync<BusinessException>(() => service.AssignCoOwner(BackOfficeUser, _submittedId, new ContactMeta { UserId = "co-2" }));
        Assert.Equal(1, await db.CaseCoOwners.CountAsync(x => x.CaseId == _submittedId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Assign_rejects_the_owner_a_final_checkpoint_a_zero_limit_and_an_unknown_case() {
        await using var db = CreateInMemoryContext();
        await Seed(db);
        var events = Substitute.For<IPlatformEventService>();
        var service = CreateAdminService(db, events, Substitute.For<ICaseContactPermissionService>(), coOwnerLimit: 5);

        await Assert.ThrowsAsync<BusinessException>(() => service.AssignCoOwner(BackOfficeUser, _submittedId, new ContactMeta { UserId = Owner }));
        await Assert.ThrowsAsync<BusinessException>(() => service.AssignCoOwner(BackOfficeUser, _submittedId, new ContactMeta { UserId = " " }));
        await Assert.ThrowsAsync<BusinessException>(() => service.AssignCoOwner(BackOfficeUser, _completedId, new ContactMeta { UserId = "co-1" }));
        Assert.False(await service.AssignCoOwner(BackOfficeUser, Guid.NewGuid(), new ContactMeta { UserId = "co-1" }));
        // working on a draft together is allowed
        Assert.True(await service.AssignCoOwner(BackOfficeUser, _draftId, new ContactMeta { UserId = "co-1" }));

        var noCoOwners = CreateAdminService(db, events, Substitute.For<ICaseContactPermissionService>(), coOwnerLimit: 0);
        await Assert.ThrowsAsync<BusinessException>(() => noCoOwners.AssignCoOwner(BackOfficeUser, _submittedId, new ContactMeta { UserId = "co-1" }));

        Assert.Empty(await db.CaseCoOwners.Where(x => x.CaseId != _draftId).ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Remove_works_on_a_finished_case_and_publishes_the_event() {
        await using var db = CreateInMemoryContext();
        await Seed(db);
        db.CaseCoOwners.Add(CoOwner(_completedId, "co-1", ContactPermission.Contributor));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var events = Substitute.For<IPlatformEventService>();
        var permissions = Substitute.For<ICaseContactPermissionService>();
        var service = CreateAdminService(db, events, permissions);

        Assert.False(await service.RemoveCoOwner(BackOfficeUser, _completedId, "someone-else"));
        Assert.True(await service.RemoveCoOwner(BackOfficeUser, _completedId, "co-1"));
        Assert.False(await service.RemoveCoOwner(BackOfficeUser, _completedId, "co-1"));

        Assert.False(await db.CaseCoOwners.AnyAsync(x => x.CaseId == _completedId, TestContext.Current.CancellationToken));
        await events.Received(1).Publish(Arg.Is<IPlatformEvent>(e => IsRemovedEvent(e, _completedId, "co-1", ContactPermission.Contributor)));
        await permissions.Received(1).Invalidate("co-1", _completedId);
    }

    [Fact]
    public async Task Owner_permission_changes_only_before_a_final_checkpoint() {
        await using var db = CreateInMemoryContext();
        await Seed(db);
        var permissions = Substitute.For<ICaseContactPermissionService>();
        var service = CreateAdminService(db, Substitute.For<IPlatformEventService>(), permissions);

        Assert.True(await service.UpdateOwnerPermission(BackOfficeUser, _submittedId, ContactPermission.Reader));
        Assert.Equal(ContactPermission.Reader, (await db.Cases.AsNoTracking().SingleAsync(c => c.Id == _submittedId, TestContext.Current.CancellationToken)).Owner.Permission);
        await permissions.Received(1).Invalidate(Owner, _submittedId);

        await Assert.ThrowsAsync<BusinessException>(() => service.UpdateOwnerPermission(BackOfficeUser, _completedId, ContactPermission.Reader));
        await Assert.ThrowsAsync<BusinessException>(() => service.UpdateOwnerPermission(BackOfficeUser, _submittedId, (ContactPermission)42));
        Assert.False(await service.UpdateOwnerPermission(BackOfficeUser, Guid.NewGuid(), ContactPermission.Reader));
    }

    [Fact]
    public async Task New_cases_start_with_a_full_owner_whatever_the_request_carries() {
        await using var db = CreateInMemoryContext();
        await Seed(db);
        var service = new MyCaseService(
            db,
            Options.Create(new CasesOptions { ReferenceNumberEnabled = false }),
            Substitute.For<IPlatformEventService>(),
            Substitute.For<IMyCaseMessageService>(),
            Substitute.For<IJsonTranslationService>(),
            new CaseSharedResourceService(Substitute.For<IStringLocalizerFactory>()));

        var created = await service.CreateDraft(new UserActor { Id = Owner, Name = "Owner", Email = "owner@example.test" }, "T", null,
            new ContactMeta { UserId = Owner, Permission = ContactPermission.Reader }, [], null, null);

        var @case = await db.Cases.AsNoTracking().SingleAsync(c => c.Id == created.Id, TestContext.Current.CancellationToken);
        Assert.Equal(ContactPermission.Owner, @case.Owner.Permission);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Permissions_decide_what_each_participant_can_do(bool useSqlServer) {
        using var host = await CreateHost(Substitute.For<IPlatformEventService>(), useSqlServer);
        using (var scope = host.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<CasesDbContext>();
            await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            await Seed(db);
            db.CaseCoOwners.AddRange(
                CoOwner(_submittedId, Reader, ContactPermission.Reader, tin: "111111111", reference: "REF-READER"),
                CoOwner(_submittedId, Contributor, ContactPermission.Contributor),
                CoOwner(_draftId, Reader, ContactPermission.Reader),
                CoOwner(_draftId, Contributor, ContactPermission.Contributor),
                CoOwner(_draftId, FullCoOwner, ContactPermission.Owner),
                CoOwner(_secondDraftId, Contributor, ContactPermission.Contributor));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        using var client = host.GetTestClient();

        // a reader can look, and a successful read does not open the door to a write
        Assert.Equal(HttpStatusCode.OK, (await Send(client, Reader, HttpMethod.Get, $"/my/cases/{_submittedId}?includeAttachmentData=false")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Send(client, Reader, HttpMethod.Get, $"/my/cases/{_submittedId}/timeline")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, Reader, HttpMethod.Post, $"/my/cases/{_submittedId}/comments", new { comment = "Hello" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, Reader, HttpMethod.Put, $"/my/cases/{_draftId}", new { data = new { name = "reader" } })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Upload(client, Reader, _draftId)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, Reader, HttpMethod.Post, $"/my/cases/{_draftId}/submit")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, Reader, HttpMethod.Delete, $"/my/cases/{_draftId}")).StatusCode);

        // the co-owners of a case are visible to its participants, without the tax number or the reference of each other
        var coOwnersResponse = await Send(client, Reader, HttpMethod.Get, $"/my/cases/{_submittedId}/co-owners");
        Assert.Equal(HttpStatusCode.OK, coOwnersResponse.StatusCode);
        var coOwners = (await coOwnersResponse.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("items");
        Assert.Equal(2, coOwners.GetArrayLength());
        var reader = Assert.Single(coOwners.EnumerateArray(), x => x.GetProperty("userId").GetString() == Reader);
        Assert.Equal("Reader", reader.GetProperty("permission").GetString());
        Assert.Equal(Reader, reader.GetProperty("firstName").GetString());
        Assert.False(reader.TryGetProperty("tin", out _));
        Assert.False(reader.TryGetProperty("reference", out _));
        Assert.False(reader.TryGetProperty("addedBy", out _));

        // a contributor can do everything except discard the case
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, Contributor, HttpMethod.Post, $"/my/cases/{_submittedId}/comments", new { comment = "Hello" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, Contributor, HttpMethod.Put, $"/my/cases/{_draftId}", new { data = new { name = "contributor" } })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Upload(client, Contributor, _draftId)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, Contributor, HttpMethod.Delete, $"/my/cases/{_draftId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, Contributor, HttpMethod.Post, $"/my/cases/{_secondDraftId}/submit")).StatusCode);

        // someone that takes no part in the case sees nothing of it
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, Stranger, HttpMethod.Get, $"/my/cases/{_submittedId}?includeAttachmentData=false")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, Stranger, HttpMethod.Get, $"/my/cases/{_submittedId}/co-owners")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(client, Stranger, HttpMethod.Get, $"/my/cases/{Guid.NewGuid()}?includeAttachmentData=false")).StatusCode);

        // the list holds the cases one owns and the ones one co-owns, at any permission (drafts are left out by default)
        // Checked on SQL Server only: the in-memory provider cannot run the rejection reason part of the list projection.
        if (useSqlServer) {
            Assert.Equal([_submittedId], await ListCases(client, Reader));
            Assert.Equal(new[] { _submittedId, _secondDraftId }.Order(), (await ListCases(client, Contributor)).Order());
            Assert.Equal(new[] { _submittedId, _secondDraftId, _completedId }.Order(), (await ListCases(client, Owner)).Order());
            Assert.Empty(await ListCases(client, Stranger));
        }

        // a co-owner that was given the owner permission can discard the draft
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, FullCoOwner, HttpMethod.Delete, $"/my/cases/{_draftId}")).StatusCode);
        using (var scope = host.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<CasesDbContext>();
            Assert.False(await db.Cases.AnyAsync(c => c.Id == _draftId, TestContext.Current.CancellationToken));
            Assert.False(await db.CaseCoOwners.AnyAsync(c => c.CaseId == _draftId, TestContext.Current.CancellationToken));
        }
    }

    [Fact]
    public async Task Back_office_changes_take_effect_on_the_next_request() {
        var events = Substitute.For<IPlatformEventService>();
        using var host = await CreateHost(events, useSqlServer: false, coOwnerLimit: 1);
        using (var scope = host.Services.CreateScope()) {
            await Seed(scope.ServiceProvider.GetRequiredService<CasesDbContext>());
        }
        using var client = host.GetTestClient();
        var comments = $"/my/cases/{_submittedId}/comments";
        var details = $"/my/cases/{_submittedId}?includeAttachmentData=false";
        var manage = $"/manage/cases/{_submittedId}";

        // only an administrator manages co-owners and permissions
        var coOwner = new { coOwner = new { userId = Stranger, firstName = "New", lastName = "Comer", tin = "222222222", reference = "REF-NEW", permission = "Contributor" } };
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, Manager, HttpMethod.Put, $"{manage}/co-owners", coOwner, BasicRoleNames.CasesManager)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, Manager, HttpMethod.Delete, $"{manage}/co-owners/{Stranger}", role: BasicRoleNames.CasesManager)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, Manager, HttpMethod.Put, $"{manage}/owner/permission", new { permission = "Reader" }, BasicRoleNames.CasesManager)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, Owner, HttpMethod.Put, $"{manage}/co-owners", coOwner)).StatusCode);

        // lowering the permission of the owner is enforced against the owner, at once
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, Owner, HttpMethod.Post, comments, new { comment = "As owner" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, Administrator, HttpMethod.Put, $"{manage}/owner/permission", new { permission = "Reader" }, BasicRoleNames.CasesAdministrator)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, Owner, HttpMethod.Post, comments, new { comment = "As reader" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Send(client, Owner, HttpMethod.Get, details)).StatusCode);

        // a user that was refused is let in right after being added
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, Stranger, HttpMethod.Get, details)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, Administrator, HttpMethod.Put, $"{manage}/co-owners", coOwner, BasicRoleNames.CasesAdministrator)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Send(client, Stranger, HttpMethod.Get, details)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, Stranger, HttpMethod.Post, comments, new { comment = "As contributor" })).StatusCode);
        await events.Received(1).Publish(Arg.Is<IPlatformEvent>(e => IsAddedEvent(e, _submittedId, Stranger, ContactPermission.Contributor)));

        // the back-office sees the co-owners on the case, with their contact details and who added them
        var caseResponse = await Send(client, Administrator, HttpMethod.Get, manage, role: BasicRoleNames.CasesAdministrator);
        Assert.Equal(HttpStatusCode.OK, caseResponse.StatusCode);
        var @case = await caseResponse.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("Reader", @case.GetProperty("ownerPermission").GetString());
        var listed = Assert.Single(@case.GetProperty("coOwners").EnumerateArray());
        Assert.Equal(Stranger, listed.GetProperty("userId").GetString());
        Assert.Equal("222222222", listed.GetProperty("tin").GetString());
        Assert.Equal("REF-NEW", listed.GetProperty("reference").GetString());
        Assert.Equal("Contributor", listed.GetProperty("permission").GetString());
        Assert.Equal(Administrator, listed.GetProperty("addedBy").GetProperty("id").GetString());
        var listResponse = await Send(client, Administrator, HttpMethod.Get, $"{manage}/co-owners", role: BasicRoleNames.CasesAdministrator);
        Assert.Equal(1, (await listResponse.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("count").GetInt32());

        // changing the permission of a co-owner is enforced at once too
        var asReader = new { coOwner = new { userId = Stranger, firstName = "New", lastName = "Comer", permission = "Reader" } };
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, Administrator, HttpMethod.Put, $"{manage}/co-owners", asReader, BasicRoleNames.CasesAdministrator)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, Stranger, HttpMethod.Post, comments, new { comment = "As reader" })).StatusCode);

        // requests that break the rules are refused
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, Administrator, HttpMethod.Put, $"{manage}/co-owners", new { coOwner = new { userId = Owner } }, BasicRoleNames.CasesAdministrator)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, Administrator, HttpMethod.Put, $"{manage}/co-owners", new { coOwner = new { userId = "another" } }, BasicRoleNames.CasesAdministrator)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, Administrator, HttpMethod.Put, $"{manage}/co-owners", new { coOwner = new { firstName = "No id" } }, BasicRoleNames.CasesAdministrator)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, Administrator, HttpMethod.Put, $"{manage}/co-owners", new { coOwner = new { userId = Stranger, permission = 7 } }, BasicRoleNames.CasesAdministrator)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(client, Administrator, HttpMethod.Put, $"/manage/cases/{Guid.NewGuid()}/co-owners", coOwner, BasicRoleNames.CasesAdministrator)).StatusCode);

        // a removed co-owner is refused on the very next request
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, Administrator, HttpMethod.Delete, $"{manage}/co-owners/{Stranger}", role: BasicRoleNames.CasesAdministrator)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, Stranger, HttpMethod.Get, details)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(client, Administrator, HttpMethod.Delete, $"{manage}/co-owners/{Stranger}", role: BasicRoleNames.CasesAdministrator)).StatusCode);
        await events.Received(1).Publish(Arg.Is<IPlatformEvent>(e => IsRemovedEvent(e, _submittedId, Stranger, ContactPermission.Reader)));
    }

    public async ValueTask DisposeAsync() {
        if (_sqlServerOptions is not null) {
            await using var db = new CasesDbContext(_sqlServerOptions);
            await db.Database.EnsureDeletedAsync();
        }
    }

    private static bool IsAddedEvent(IPlatformEvent @event, Guid caseId, string userId, ContactPermission permission) =>
        @event is CaseCoOwnerAddedEvent added &&
        added.CaseId == caseId && added.CaseTypeCode == "T" &&
        added.CoOwner.UserId == userId && added.CoOwner.Permission == permission &&
        added.AddedBy.Id == Administrator && added.AddedBy.When is not null;

    private static bool IsRemovedEvent(IPlatformEvent @event, Guid caseId, string userId, ContactPermission permission) =>
        @event is CaseCoOwnerRemovedEvent removed &&
        removed.CaseId == caseId && removed.CaseTypeCode == "T" &&
        removed.CoOwner.UserId == userId && removed.CoOwner.Permission == permission &&
        removed.RemovedBy.Id == Administrator && removed.RemovedBy.When is not null;

    private static CasesDbContext CreateInMemoryContext() =>
        new(new DbContextOptionsBuilder<CasesDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static AdminCaseService CreateAdminService(CasesDbContext db, IPlatformEventService events, ICaseContactPermissionService permissions, int coOwnerLimit = 1) => new(
        db,
        Options.Create(new CasesOptions { CoOwnerLimit = coOwnerLimit }),
        Substitute.For<ICaseAuthorizationProvider>(),
        Substitute.For<IAdminCaseMessageService>(),
        events,
        permissions,
        NullLogger<AdminCaseService>.Instance);

    private static AuditMeta Audit(string id) => new() { Id = id, Name = id, Email = $"{id}@example.test", When = DateTimeOffset.UtcNow };

    private static DbCoownsCase CoOwner(Guid caseId, string userId, ContactPermission permission, string? tin = null, string? reference = null) => new() {
        Id = Guid.NewGuid(),
        CaseId = caseId,
        CoOwner = new CoOwnerContactMeta { UserId = userId, FirstName = userId, LastName = "Doe", Tin = tin, Reference = reference, Permission = permission, AddedBy = Audit(Administrator) }
    };

    /// <summary>Two drafts, a submitted case and a completed one. All of them belong to the same owner, who also created them.</summary>
    private async Task Seed(CasesDbContext db) {
        var caseType = new DbCaseType { Id = Guid.NewGuid(), Code = "T", Title = "Test", DataSchema = JsonNode.Parse("{}")! };
        var open = new DbCheckpointType { Id = Guid.NewGuid(), CaseTypeId = caseType.Id, Code = "Open", Status = CaseStatus.InProgress };
        var completed = new DbCheckpointType { Id = Guid.NewGuid(), CaseTypeId = caseType.Id, Code = "Completed", Status = CaseStatus.Completed };
        db.CaseTypes.Add(caseType);
        db.CheckpointTypes.AddRange(open, completed);
        (Guid Id, bool Draft, DbCheckpointType CheckpointType)[] cases = [
            (_draftId, true, open),
            (_secondDraftId, true, open),
            (_submittedId, false, open),
            (_completedId, false, completed)
        ];
        var entities = cases.Select(x => new DbCase {
            Id = x.Id,
            CaseTypeId = caseType.Id,
            Channel = "web",
            Draft = x.Draft,
            CreatedBy = Audit(Owner),
            Owner = new ContactMeta { UserId = Owner, FirstName = "Olivia", LastName = "Owner", Tin = "999999999", Permission = ContactPermission.Owner }
        }).ToList();
        db.Cases.AddRange(entities);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // The case and its current checkpoint and data point at each other, so they are linked in a second step.
        foreach (var (entity, checkpointType) in entities.Zip(cases.Select(x => x.CheckpointType))) {
            var checkpoint = new DbCheckpoint { Id = Guid.NewGuid(), CaseId = entity.Id, CheckpointTypeId = checkpointType.Id, CreatedBy = Audit(Owner) };
            var data = new DbCaseData { Id = Guid.NewGuid(), CaseId = entity.Id, CreatedBy = Audit(Owner), Data = JsonNode.Parse("{}") };
            db.Checkpoints.Add(checkpoint);
            db.CaseData.Add(data);
            entity.CheckpointId = checkpoint.Id;
            entity.PublicCheckpointId = checkpoint.Id;
            entity.DataId = data.Id;
            entity.PublicDataId = data.Id;
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task<IHost> CreateHost(IPlatformEventService events, bool useSqlServer, int coOwnerLimit = 1) {
        var database = Guid.NewGuid().ToString();
        var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database=Indice.Features.Cases.CoOwners_{database};Trusted_Connection=True;MultipleActiveResultSets=true";
        if (useSqlServer) {
            _sqlServerOptions = new DbContextOptionsBuilder<CasesDbContext>().UseSqlServer(connectionString).Options;
        }
        return await new HostBuilder().ConfigureWebHost(builder => {
            builder.UseTestServer();
            builder.ConfigureServices(services => {
                services.AddRouting();
                services.Configure<CaseServerOptions>(_ => { });
                services.Configure<CasesOptions>(options => {
                    options.CoOwnerLimit = coOwnerLimit;
                    options.ReferenceNumberEnabled = false;
                });
                services.AddLogging();
                services.AddHttpContextAccessor();
                services.AddDistributedMemoryCache();
                services.AddLimitUpload();
                services.AddEndpointParameterFluentValidation();
                services.AddValidatorsFromAssemblyContaining<AssignCoOwnerRequestValidators>();
                services.AddDbContext<CasesDbContext>(o => {
                    if (useSqlServer) {
                        o.UseSqlServer(connectionString);
                    } else {
                        o.UseInMemoryDatabase(database);
                    }
                });
                services.AddAuthentication("Bearer").AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Bearer", _ => { });
                services.AddAuthorization();
                services.AddTransient<IAuthorizationHandler, CasesAccessOwnerHandler>();
                services.AddTransient<IAuthorizationHandler, CasesAccessRoleBasedHandler>();
                services.AddTransient<IAuthorizationHandler, CasesAccessMemberHandler>();
                services.AddTransient<ICaseContactPermissionService, CaseContactPermissionService>();
                services.AddSingleton(events);
                services.AddSingleton(Substitute.For<ICaseAuthorizationProvider>());
                services.AddSingleton(Substitute.For<IAdminCaseMessageService>());
                services.AddSingleton(Substitute.For<IJsonTranslationService>());
                services.AddSingleton(Substitute.For<ICaseTemplateService>());
                services.AddSingleton(Substitute.For<ICasePdfService>());
                services.AddSingleton(Substitute.For<ICasesWorkflowManager>());
                services.AddSingleton(Substitute.For<ICaseActionsService>());
                services.AddSingleton(Substitute.For<ICaseApprovalService>());
                services.AddSingleton(Substitute.For<ICaseDataInitializer>());
                services.AddSingleton(new CaseSharedResourceService(Substitute.For<IStringLocalizerFactory>()));
                services.AddTransient<ISchemaValidator, CasesJsonSchemaValidator>();
                services.AddTransient<IMyCaseService, MyCaseService>();
                services.AddTransient<IMyCaseMessageService, MyCaseMessageService>();
                services.AddTransient<IAdminCaseService, AdminCaseService>();
            });
            builder.Configure(app => {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints => {
                    endpoints.MapMyCases();
                    endpoints.MapAdminCases();
                });
            });
        }).StartAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<HttpResponseMessage> Send(HttpClient client, string user, HttpMethod method, string url, object? body = null, string? role = null) {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Test-User", user);
        if (role is not null) {
            request.Headers.Add("X-Test-Role", role);
        }
        if (body is not null) {
            request.Content = JsonContent.Create(body, options: Json);
        }
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static async Task<HttpResponseMessage> Upload(HttpClient client, string user, Guid caseId) {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/my/cases/{caseId}/attachments");
        request.Headers.Add("X-Test-User", user);
        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent([1, 2, 3]) { Headers = { ContentType = new("text/plain") } }, "file", "note.txt");
        request.Content = content;
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static async Task<Guid[]> ListCases(HttpClient client, string user) {
        var response = await Send(client, user, HttpMethod.Get, "/my/cases");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        return [.. result.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetGuid())];
    }

    private sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
            var user = Request.Headers["X-Test-User"].ToString();
            if (string.IsNullOrEmpty(user)) {
                return Task.FromResult(AuthenticateResult.NoResult());
            }
            var claims = new List<Claim> {
                new(BasicClaimTypes.Subject, user),
                new(BasicClaimTypes.Scope, CasesCoreConstants.DefaultScopeName),
                new(BasicClaimTypes.Email, $"{user}@example.test"),
                new(BasicClaimTypes.GivenName, user),
                new(BasicClaimTypes.FamilyName, "Tester")
            };
            var role = Request.Headers["X-Test-Role"].ToString();
            if (!string.IsNullOrEmpty(role)) {
                claims.Add(new(BasicClaimTypes.Role, role));
            }
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
        }
    }
}
