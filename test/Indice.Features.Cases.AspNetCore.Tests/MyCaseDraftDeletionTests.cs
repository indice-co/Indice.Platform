using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
using Indice.Events;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Localization;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Services;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Features.Cases.Server;
using Indice.Features.Cases.Server.Authorization;
using Indice.Services;
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
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Indice.Features.Cases.Tests;

public class MyCaseDraftDeletionTests : IAsyncDisposable
{
    private DbContextOptions<CasesDbContext>? _sqlServerOptions;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Delete_discards_owned_draft_and_children_and_returns_expected_statuses(bool useSqlServer) {
        var database = Guid.NewGuid().ToString();
        var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database=Indice.Features.Cases.DraftDeletion_{database};Trusted_Connection=True;MultipleActiveResultSets=true";
        if (useSqlServer) {
            _sqlServerOptions = new DbContextOptionsBuilder<CasesDbContext>()
                .UseSqlServer(connectionString)
                .Options;
        }
        var draftId = Guid.NewGuid();
        var submittedId = Guid.NewGuid();
        var attachmentContent = new byte[] { 1, 2, 3, 4 };
        using var host = await new HostBuilder().ConfigureWebHost(builder => {
            builder.UseTestServer();
            builder.ConfigureServices(services => {
                services.AddRouting();
                services.Configure<CaseServerOptions>(_ => { });
                services.Configure<CasesOptions>(_ => { });
                services.AddLogging();
                services.AddHttpContextAccessor();
                services.AddDistributedMemoryCache();
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
                services.AddSingleton(Substitute.For<ICaseAuthorizationProvider>());
                services.AddSingleton(Substitute.For<IPlatformEventService>());
                services.AddSingleton(Substitute.For<IMyCaseMessageService>());
                services.AddSingleton(Substitute.For<IJsonTranslationService>());
                services.AddSingleton(Substitute.For<ICaseTemplateService>());
                services.AddSingleton(Substitute.For<ICasePdfService>());
                services.AddSingleton(new CaseSharedResourceService(Substitute.For<IStringLocalizerFactory>()));
                services.AddTransient<IMyCaseService, MyCaseService>();
                services.AddFiles(builder => builder.AddFilesInMemory(CasesCoreConstants.FileServiceKey));
            });
            builder.Configure(app => {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints => endpoints.MapMyCases());
            });
        }).StartAsync();

        var attachmentPath = string.Empty;
        using (var scope = host.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<CasesDbContext>();
            await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            var fileService = scope.ServiceProvider.GetRequiredKeyedService<IFileService>(CasesCoreConstants.FileServiceKey);
            var caseType = new DbCaseType { Id = Guid.NewGuid(), Code = "T", Title = "Test", DataSchema = JsonNode.Parse("{}")! };
            var checkpointType = new DbCheckpointType { Id = Guid.NewGuid(), CaseTypeId = caseType.Id, Code = "Draft" };
            var now = DateTimeOffset.UtcNow;
            AuditMeta CreatedBy(string id) => new() { Id = id, Name = id, Email = $"{id}@example.test", When = now };
            var draftCheckpoint = new DbCheckpoint { Id = Guid.NewGuid(), CaseId = draftId, CheckpointTypeId = checkpointType.Id, CreatedBy = CreatedBy("creator") };
            var attachment = new DbAttachment(draftId);
            using (var stream = new MemoryStream(attachmentContent)) {
                attachment.PopulateFrom("draft.txt", stream);
                await fileService.SaveAsync(attachment.GetPath(), stream, attachment.ContentType);
            }
            attachmentPath = attachment.GetPath();
            var comment = new DbComment { Id = Guid.NewGuid(), CaseId = draftId, AttachmentId = attachment.Id, CreatedBy = CreatedBy("creator"), Text = "Draft comment" };
            var version = new DbCaseData { Id = Guid.NewGuid(), CaseId = draftId, CreatedBy = CreatedBy("creator"), Data = JsonNode.Parse("{\"draft\":true}") };
            db.CaseTypes.Add(caseType);
            db.CheckpointTypes.Add(checkpointType);
            var draft = new DbCase { Id = draftId, CaseTypeId = caseType.Id, Channel = "web", Draft = true, CreatedBy = CreatedBy("creator"), Owner = new ContactMeta { UserId = "owner" } };
            db.Cases.AddRange(
                draft,
                new DbCase { Id = submittedId, CaseTypeId = caseType.Id, Channel = "web", Draft = false, CreatedBy = CreatedBy("owner"), Owner = new ContactMeta { UserId = "owner" } });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            db.Checkpoints.Add(draftCheckpoint);
            db.Attachments.Add(attachment);
            db.Comments.Add(comment);
            db.CaseData.Add(version);
            db.CaseApprovals.Add(new DbCaseApproval { Id = Guid.NewGuid(), CaseId = draftId, CommentId = comment.Id, CreatedBy = CreatedBy("creator") });
            db.CaseAccessRules.Add(new DbCaseAccessRule { Id = Guid.NewGuid(), RuleCaseId = draftId, MemberUserId = "delegate", AccessLevel = 0 });
            draft.CheckpointId = draftCheckpoint.Id;
            draft.PublicCheckpointId = draftCheckpoint.Id;
            draft.DataId = version.Id;
            draft.PublicDataId = version.Id;
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using var client = host.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "owner");
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/my/cases/{Guid.NewGuid()}", TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.DeleteAsync($"/my/cases/{submittedId}", TestContext.Current.CancellationToken)).StatusCode);
        client.DefaultRequestHeaders.Remove("X-Test-User");
        client.DefaultRequestHeaders.Add("X-Test-User", "unauthorized");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync($"/my/cases/{draftId}", TestContext.Current.CancellationToken)).StatusCode);
        client.DefaultRequestHeaders.Remove("X-Test-User");
        client.DefaultRequestHeaders.Add("X-Test-User", "owner");
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/my/cases/{draftId}", TestContext.Current.CancellationToken)).StatusCode);

        using (var scope = host.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<CasesDbContext>();
            Assert.False(await db.Cases.AnyAsync(c => c.Id == draftId, TestContext.Current.CancellationToken));
            Assert.False(await db.Checkpoints.AnyAsync(c => c.CaseId == draftId, TestContext.Current.CancellationToken));
            Assert.False(await db.Comments.AnyAsync(c => c.CaseId == draftId, TestContext.Current.CancellationToken));
            Assert.False(await db.Attachments.AnyAsync(c => c.CaseId == draftId, TestContext.Current.CancellationToken));
            Assert.False(await db.CaseData.AnyAsync(c => c.CaseId == draftId, TestContext.Current.CancellationToken));
            Assert.False(await db.CaseApprovals.AnyAsync(c => c.CaseId == draftId, TestContext.Current.CancellationToken));
            Assert.False(await db.CaseAccessRules.AnyAsync(c => c.RuleCaseId == draftId, TestContext.Current.CancellationToken));
            Assert.True(await db.Cases.AnyAsync(c => c.Id == submittedId, TestContext.Current.CancellationToken));
            var fileService = scope.ServiceProvider.GetRequiredKeyedService<IFileService>(CasesCoreConstants.FileServiceKey);
            var missingFile = await Assert.ThrowsAsync<Exception>(() => fileService.GetAsync(attachmentPath));
            Assert.Contains("not found", missingFile.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    public async ValueTask DisposeAsync() {
        if (_sqlServerOptions is not null) {
            await using var db = new CasesDbContext(_sqlServerOptions);
            await db.Database.EnsureDeletedAsync();
        }
    }

    private sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
            var user = Request.Headers["X-Test-User"].ToString();
            if (string.IsNullOrEmpty(user)) {
                return Task.FromResult(AuthenticateResult.NoResult());
            }
            var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", user)], Scheme.Name));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
        }
    }
}
