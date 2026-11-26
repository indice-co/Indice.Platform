using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Indice.Events;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Events;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Services;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Features.Cases.Core.Services.CaseMessageService;
using Indice.Features.Cases.Server;
using Indice.Features.Cases.Server.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Indice.Features.Cases.Tests;

public class MyCaseCommentAttachmentTests
{
    [Theory]
    [InlineData(true, AttachmentScanStatus.Pending, false)]
    [InlineData(true, AttachmentScanStatus.Unsafe, false)]
    [InlineData(true, AttachmentScanStatus.ScanFailed, false)]
    [InlineData(true, AttachmentScanStatus.Safe, true)]
    [InlineData(false, AttachmentScanStatus.Pending, true)]
    [InlineData(false, AttachmentScanStatus.Unsafe, true)]
    [InlineData(false, AttachmentScanStatus.ScanFailed, true)]
    [InlineData(false, AttachmentScanStatus.Safe, true)]
    public async Task Comment_attachment_requires_safe_status_only_when_scanning_enabled(bool scanningEnabled, AttachmentScanStatus status, bool allowed) {
        var database = Guid.NewGuid().ToString();
        var caseId = Guid.NewGuid();
        var otherCaseId = Guid.NewGuid();
        var attachmentId = Guid.NewGuid();
        var otherAttachmentId = Guid.NewGuid();
        var events = Substitute.For<IPlatformEventService>();
        using var host = await new HostBuilder().ConfigureWebHost(builder => {
            builder.UseTestServer();
            builder.ConfigureServices(services => {
                services.AddRouting();
                services.Configure<CasesOptions>(options => options.AttachmentScanningEnabled = scanningEnabled);
                services.AddLogging();
                services.AddHttpContextAccessor();
                services.AddDistributedMemoryCache();
                services.AddDbContext<CasesDbContext>(options => options.UseInMemoryDatabase(database));
                services.AddAuthentication("Bearer").AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Bearer", _ => { });
                services.AddAuthorization();
                services.AddTransient<IAuthorizationHandler, CasesAccessOwnerHandler>();
                services.AddTransient<ICaseContactPermissionService, CaseContactPermissionService>();
                services.AddSingleton(Substitute.For<ICaseAuthorizationProvider>());
                services.AddSingleton(events);
                services.AddSingleton(Substitute.For<ISchemaValidator>());
                services.AddSingleton(Substitute.For<IMyCaseService>());
                services.AddSingleton(Substitute.For<ICaseTemplateService>());
                services.AddSingleton(Substitute.For<ICasePdfService>());
                services.AddTransient<IMyCaseMessageService, MyCaseMessageService>();
            });
            builder.Configure(app => {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints => endpoints.MapMyCases());
            });
        }).StartAsync();

        using (var scope = host.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<CasesDbContext>();
            var type = new DbCaseType { Id = Guid.NewGuid(), Code = "T", Title = "Test", DataSchema = "{}" };
            var checkpointType = new DbCheckpointType { Id = Guid.NewGuid(), CaseTypeId = type.Id, Code = "OPEN" };
            var audit = new AuditMeta { Id = "owner", Name = "Owner", Email = "owner@example.test", When = DateTimeOffset.UtcNow };
            var checkpoint = new DbCheckpoint { Id = Guid.NewGuid(), CaseId = caseId, CheckpointTypeId = checkpointType.Id, CreatedBy = audit.Clone() };
            db.CaseTypes.Add(type);
            db.CheckpointTypes.Add(checkpointType);
            db.Cases.AddRange(
                new DbCase { Id = caseId, CaseTypeId = type.Id, Channel = "External", Draft = false, CreatedBy = audit, Owner = new ContactMeta { UserId = "owner", Permission = ContactPermission.Owner }, CheckpointId = checkpoint.Id },
                new DbCase { Id = otherCaseId, CaseTypeId = type.Id, Channel = "External", Draft = false, CreatedBy = audit.Clone(), Owner = new ContactMeta { UserId = "owner", Permission = ContactPermission.Owner } });
            db.Checkpoints.Add(checkpoint);
            db.Attachments.AddRange(
                new DbAttachment(caseId) { Id = attachmentId, Name = "document.txt", FileExtension = ".txt", ContentType = "text/plain", ScanStatus = status },
                new DbAttachment(otherCaseId) { Id = otherAttachmentId, Name = "other.txt", FileExtension = ".txt", ContentType = "text/plain", ScanStatus = AttachmentScanStatus.Safe });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using var client = host.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "owner");
        var wrongCase = await client.PostAsJsonAsync($"/my/cases/{caseId}/comments", new { comment = "Wrong case", attachmentId = otherAttachmentId }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, wrongCase.StatusCode);
        Assert.Equal("application/problem+json", wrongCase.Content.Headers.ContentType!.MediaType);
        var missing = await client.PostAsJsonAsync($"/my/cases/{caseId}/comments", new { comment = "Missing", attachmentId = Guid.NewGuid() }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        var response = await client.PostAsJsonAsync($"/my/cases/{caseId}/comments", new { comment = "Hello", attachmentId }, TestContext.Current.CancellationToken);
        Assert.Equal(allowed ? HttpStatusCode.NoContent : HttpStatusCode.BadRequest, response.StatusCode);
        if (!allowed) {
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
            Assert.Contains("marked safe", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }
        using (var scope = host.Services.CreateScope()) {
            var comments = await scope.ServiceProvider.GetRequiredService<CasesDbContext>().Comments.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
            if (allowed) {
                var comment = Assert.Single(comments);
                Assert.Equal(attachmentId, comment.AttachmentId);
                Assert.Equal("Hello", comment.Text);
                Assert.True(comment.IsCustomer);
                Assert.False(comment.Private);
                await events.Received(1).Publish(Arg.Any<CaseMessageCreatedEvent>());
                await events.Received(1).Publish(Arg.Any<CaseMessageSentEvent>());
            } else {
                Assert.Empty(comments);
                await events.DidNotReceive().Publish(Arg.Any<CaseMessageCreatedEvent>());
                await events.DidNotReceive().Publish(Arg.Any<CaseMessageSentEvent>());
            }
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
            var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", user), new Claim("email", $"{user}@example.test"), new Claim("name", user)], Scheme.Name));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
        }
    }
}
