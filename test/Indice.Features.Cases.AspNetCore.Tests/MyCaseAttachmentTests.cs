using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Indice.Events;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Events;
using Indice.Features.Cases.Core.Exceptions;
using Indice.Features.Cases.Core.Localization;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Features.Cases.Server.Authorization;
using Indice.Services;
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
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Indice.Features.Cases.Tests;

public class MyCaseAttachmentTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Customer_download_and_details_respect_ownership_and_content_option(bool fileServiceEnabled, bool scanningEnabled) {
        var database = Guid.NewGuid().ToString();
        var caseId = Guid.NewGuid();
        var otherCaseId = Guid.NewGuid();
        var content = Encoding.UTF8.GetBytes("case document");
        using var host = await new HostBuilder().ConfigureWebHost(builder => {
            builder.UseTestServer();
            builder.ConfigureServices(services => {
                services.AddRouting();
                services.AddOptions();
                services.Configure<CasesOptions>(options => options.AttachmentScanningEnabled = scanningEnabled);
                services.AddLogging();
                services.AddHttpContextAccessor();
                services.AddDistributedMemoryCache();
                services.AddDbContext<CasesDbContext>(o => o.UseInMemoryDatabase(database));
                services.AddAuthentication("Bearer").AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Bearer", _ => { });
                services.AddAuthorization();
                services.AddTransient<IAuthorizationHandler, CasesAccessOwnerHandler>();
                services.AddTransient<ICaseContactPermissionService, CaseContactPermissionService>();
                services.AddSingleton(Substitute.For<ICaseAuthorizationProvider>());
                services.AddSingleton(Substitute.For<IPlatformEventService>());
                services.AddSingleton(Substitute.For<IMyCaseMessageService>());
                services.AddSingleton(Substitute.For<IAdminCaseMessageService>());
                services.AddSingleton(Substitute.For<IJsonTranslationService>());
                services.AddSingleton(Substitute.For<ICaseTemplateService>());
                services.AddSingleton(Substitute.For<ICasePdfService>());
                services.AddSingleton(new CaseSharedResourceService(Substitute.For<IStringLocalizerFactory>()));
                services.AddTransient<IMyCaseService, MyCaseService>();
                services.AddTransient<IAdminCaseService, AdminCaseService>();
                services.AddTransient<ICaseAttachmentScanService, CaseAttachmentScanService>();
                if (fileServiceEnabled) {
                    services.AddFiles(builder => builder.AddFilesInMemory(CasesCoreConstants.FileServiceKey));
                }
            });
            builder.Configure(app => {
                app.UseRequestLocalization(new RequestLocalizationOptions()
                    .SetDefaultCulture("en-US")
                    .AddSupportedCultures("en-US", "el-GR")
                    .AddSupportedUICultures("en-US", "el-GR"));
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints => endpoints.MapMyCases());
            });
        }).StartAsync();

        Guid attachmentId;
        Guid otherAttachmentId;
        using (var scope = host.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<CasesDbContext>();
            var caseType = new DbCaseType { Id = Guid.NewGuid(), Code = "T", Title = "Type", DataSchema = "{}" };
            var checkpointType = new DbCheckpointType {
                Id = Guid.NewGuid(), CaseTypeId = caseType.Id, Code = "OPEN", Title = "Draft title", Description = "Draft description",
                Translations = new TranslationDictionary<CheckpointTypeTranslation> {
                    ["el"] = new() { Title = "Translated draft", Description = "Translated draft description" }
                }
            };
            var publicType = new DbCheckpointType {
                Id = Guid.NewGuid(), CaseTypeId = caseType.Id, Code = "PUBLIC", Title = "Public title", Description = "Public description",
                Translations = new TranslationDictionary<CheckpointTypeTranslation> {
                    ["el"] = new() { Title = "Translated public", Description = "Translated public description" }
                }
            };
            var audit = new AuditMeta { Id = "owner", Name = "Owner", Email = "owner@example.test", When = DateTimeOffset.UtcNow };
            var checkpoint = new DbCheckpoint { Id = Guid.NewGuid(), CaseId = caseId, CheckpointTypeId = checkpointType.Id, CreatedBy = audit.Clone() };
            var publicCheckpoint = new DbCheckpoint { Id = Guid.NewGuid(), CaseId = otherCaseId, CheckpointTypeId = publicType.Id, CreatedBy = audit.Clone() };
            var privateCheckpoint = new DbCheckpoint { Id = Guid.NewGuid(), CaseId = otherCaseId, CheckpointTypeId = checkpointType.Id, CreatedBy = audit.Clone() };
            var version = new DbCaseData { CaseId = caseId, CreatedBy = audit.Clone(), Data = System.Text.Json.Nodes.JsonNode.Parse("{}") };
            var publicVersion = new DbCaseData { CaseId = otherCaseId, CreatedBy = audit.Clone(), Data = System.Text.Json.Nodes.JsonNode.Parse("{}") };
            db.CaseTypes.Add(caseType);
            db.CheckpointTypes.AddRange(checkpointType, publicType);
            db.Cases.Add(new DbCase { Id = caseId, CaseTypeId = caseType.Id, Channel = "web", Draft = true, CreatedBy = audit, Owner = new ContactMeta { UserId = "owner", Permission = ContactPermission.Owner }, CheckpointId = checkpoint.Id, DataId = version.Id });
            db.Cases.Add(new DbCase { Id = otherCaseId, CaseTypeId = caseType.Id, Channel = "web", Draft = false, CreatedBy = audit.Clone(), Owner = new ContactMeta { UserId = "owner", Permission = ContactPermission.Owner }, CheckpointId = privateCheckpoint.Id, PublicCheckpointId = publicCheckpoint.Id, PublicDataId = publicVersion.Id });
            db.Checkpoints.AddRange(checkpoint, publicCheckpoint, privateCheckpoint);
            db.CaseData.AddRange(version, publicVersion);
            var attachment = new DbAttachment(caseId);
            using var stream = new MemoryStream(content);
            attachment.PopulateFrom("document.txt", stream, saveData: !fileServiceEnabled);
            if (fileServiceEnabled) {
                await scope.ServiceProvider.GetRequiredKeyedService<IFileService>(CasesCoreConstants.FileServiceKey).SaveAsync(attachment.GetPath(), stream, attachment.ContentType);
            }
            var otherAttachment = new DbAttachment(otherCaseId) { Name = "other.txt", FileExtension = ".txt", ContentType = "text/plain", Data = content };
            attachmentId = attachment.Id;
            otherAttachmentId = otherAttachment.Id;
            version.Data = System.Text.Json.Nodes.JsonNode.Parse($"{{\"document\":\"{attachment.Id}\"}}");
            db.Attachments.AddRange(attachment, otherAttachment);
            await db.SaveChangesAsync();
        }

        using var client = host.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "owner");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("el-GR");
        foreach (var status in new[] { AttachmentScanStatus.Pending, AttachmentScanStatus.Unsafe, AttachmentScanStatus.ScanFailed }) {
            using (var scope = host.Services.CreateScope()) {
                var scanner = scope.ServiceProvider.GetRequiredService<ICaseAttachmentScanService>();
                Assert.True(await scanner.SetStatus(attachmentId, status, "scanner"));
                var admin = scope.ServiceProvider.GetRequiredService<IAdminCaseService>();
                var adminCase = await admin.GetCaseById(caseId, fetchPublicData: false, includeAttachmentData: true);
                var adminAttachment = Assert.Single(adminCase!.Attachments);
                Assert.Equal(status, adminAttachment.ScanStatus);
                Assert.Equal(scanningEnabled ? null : content, adminAttachment.Data);
                Assert.Equal(status, Assert.Single((await admin.GetAttachments(caseId)).Items).ScanStatus);
                if (scanningEnabled) {
                    await Assert.ThrowsAsync<AttachmentNotSafeException>(() => admin.GetAttachment(caseId, attachmentId));
                    await Assert.ThrowsAsync<AttachmentNotSafeException>(() => admin.GetAttachmentById(attachmentId));
                    await Assert.ThrowsAsync<AttachmentNotSafeException>(() => admin.GetAttachmentByField(new UserActor { Id = "owner" }, caseId, "document"));
                } else {
                    Assert.Equal(content, (await admin.GetAttachment(caseId, attachmentId))!.Data);
                    Assert.Equal(content, (await admin.GetAttachmentById(attachmentId))!.Data);
                    Assert.Equal(content, (await admin.GetAttachmentByField(new UserActor { Id = "owner" }, caseId, "document"))!.Data);
                }
            }
            var blockedDownload = await client.GetAsync($"/my/cases/{caseId}/attachments/{attachmentId}");
            Assert.Equal(scanningEnabled ? HttpStatusCode.Conflict : HttpStatusCode.OK, blockedDownload.StatusCode);
            if (scanningEnabled) {
                Assert.Equal("application/problem+json", blockedDownload.Content.Headers.ContentType!.MediaType);
            }
            var detailsResponse = await client.GetAsync($"/my/cases/{caseId}");
            Assert.Equal(HttpStatusCode.OK, detailsResponse.StatusCode);
            var details = await detailsResponse.Content.ReadFromJsonAsync<JsonElement>();
            var item = Assert.Single(details.GetProperty("attachments").EnumerateArray());
            Assert.Equal(status.ToString(), item.GetProperty("scanStatus").GetString());
            if (scanningEnabled) {
                Assert.True(!item.TryGetProperty("data", out var unsafeData) || unsafeData.ValueKind == JsonValueKind.Null);
            } else {
                Assert.Equal(content, item.GetProperty("data").GetBytesFromBase64());
            }
        }
        using (var scope = host.Services.CreateScope()) {
            var scanner = scope.ServiceProvider.GetRequiredService<ICaseAttachmentScanService>();
            Assert.True(await scanner.SetStatus(attachmentId, AttachmentScanStatus.Safe, "scanner"));
            Assert.True(await scanner.SetStatus(attachmentId, AttachmentScanStatus.Safe, "scanner"));
            Assert.False(await scanner.SetStatus(Guid.NewGuid(), AttachmentScanStatus.Safe, "scanner"));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => scanner.SetStatus(attachmentId, (AttachmentScanStatus)999, "scanner"));
            await Assert.ThrowsAsync<ArgumentException>(() => scanner.SetStatus(attachmentId, AttachmentScanStatus.Safe, " "));
            var db = scope.ServiceProvider.GetRequiredService<CasesDbContext>();
            var scanned = await db.Attachments.SingleAsync(x => x.Id == attachmentId);
            Assert.Equal("scanner", scanned.ScanStatusChangedBy);
            Assert.NotNull(scanned.ScanStatusChangedAt);
            var events = scope.ServiceProvider.GetRequiredService<IPlatformEventService>();
            await events.Received(1).Publish(Arg.Is<IPlatformEvent>(e => e is CaseAttachmentScannedEvent &&
                ((CaseAttachmentScannedEvent)e).AttachmentId == attachmentId &&
                ((CaseAttachmentScannedEvent)e).CaseId == caseId &&
                ((CaseAttachmentScannedEvent)e).PreviousStatus == AttachmentScanStatus.ScanFailed &&
                ((CaseAttachmentScannedEvent)e).Status == AttachmentScanStatus.Safe &&
                ((CaseAttachmentScannedEvent)e).ScannerId == "scanner"));
        }
        var download = await client.GetAsync($"/my/cases/{caseId}/attachments/{attachmentId}");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal(content, await download.Content.ReadAsByteArrayAsync());
        Assert.Equal("text/plain", download.Content.Headers.ContentType!.MediaType);
        Assert.Contains("document.txt", download.Content.Headers.ContentDisposition!.ToString());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/my/cases/{caseId}/attachments/{otherAttachmentId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/my/cases/{caseId}/attachments/{Guid.NewGuid()}")).StatusCode);
        foreach (var query in new[] { "", "?includeAttachmentData=true", "?includeAttachmentData=false" }) {
            var response = await client.GetAsync($"/my/cases/{caseId}{query}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var checkpoint = body.GetProperty("checkpointType");
            Assert.Equal("Translated draft", checkpoint.GetProperty("title").GetString());
            Assert.Equal("Translated draft description", checkpoint.GetProperty("description").GetString());
            Assert.True(checkpoint.GetProperty("translations").TryGetProperty("el", out _));
            var attachment = Assert.Single(body.GetProperty("attachments").EnumerateArray());
            Assert.Equal("document.txt", attachment.GetProperty("fileName").GetString());
            if (query.EndsWith("false")) {
                Assert.True(!attachment.TryGetProperty("data", out var data) || data.ValueKind == JsonValueKind.Null);
            } else {
                Assert.Equal(content, attachment.GetProperty("data").GetBytesFromBase64());
            }
        }
        var publicResponse = await client.GetAsync($"/my/cases/{otherCaseId}?includeAttachmentData=false");
        Assert.Equal(HttpStatusCode.OK, publicResponse.StatusCode);
        var publicBody = await publicResponse.Content.ReadFromJsonAsync<JsonElement>();
        var publicCheckpointResponse = publicBody.GetProperty("checkpointType");
        Assert.Equal("PUBLIC", publicCheckpointResponse.GetProperty("code").GetString());
        Assert.Equal("Translated public", publicCheckpointResponse.GetProperty("title").GetString());
        Assert.Equal("Translated public description", publicCheckpointResponse.GetProperty("description").GetString());

        client.DefaultRequestHeaders.AcceptLanguage.Clear();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US");
        var fallbackResponse = await client.GetAsync($"/my/cases/{otherCaseId}?includeAttachmentData=false");
        Assert.Equal(HttpStatusCode.OK, fallbackResponse.StatusCode);
        var fallbackBody = await fallbackResponse.Content.ReadFromJsonAsync<JsonElement>();
        var fallbackCheckpoint = fallbackBody.GetProperty("checkpointType");
        Assert.Equal("Public title", fallbackCheckpoint.GetProperty("title").GetString());
        Assert.Equal("Public description", fallbackCheckpoint.GetProperty("description").GetString());

        client.DefaultRequestHeaders.Remove("X-Test-User");
        client.DefaultRequestHeaders.Add("X-Test-User", "other");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/my/cases/{caseId}/attachments/{attachmentId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/my/cases/{caseId}?includeAttachmentData=false")).StatusCode);
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
