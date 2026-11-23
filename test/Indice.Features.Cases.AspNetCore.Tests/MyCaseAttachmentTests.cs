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
using Indice.Features.Cases.Core.Localization;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Services;
using Indice.Features.Cases.Core.Services.Abstractions;
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

public class MyCaseAttachmentTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Customer_download_and_details_respect_ownership_and_content_option(bool fileServiceEnabled) {
        var database = Guid.NewGuid().ToString();
        var caseId = Guid.NewGuid();
        var otherCaseId = Guid.NewGuid();
        var content = Encoding.UTF8.GetBytes("case document");
        using var host = await new HostBuilder().ConfigureWebHost(builder => {
            builder.UseTestServer();
            builder.ConfigureServices(services => {
                services.AddRouting();
                services.AddOptions();
                services.AddLogging();
                services.AddHttpContextAccessor();
                services.AddDistributedMemoryCache();
                services.AddDbContext<CasesDbContext>(o => o.UseInMemoryDatabase(database));
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
                if (fileServiceEnabled) {
                    services.AddFiles(builder => builder.AddFilesInMemory(CasesCoreConstants.FileServiceKey));
                }
            });
            builder.Configure(app => {
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
            var checkpointType = new DbCheckpointType { Id = Guid.NewGuid(), CaseTypeId = caseType.Id, Code = "OPEN" };
            var audit = new AuditMeta { Id = "owner", Name = "Owner", Email = "owner@example.test", When = DateTimeOffset.UtcNow };
            var checkpoint = new DbCheckpoint { Id = Guid.NewGuid(), CaseId = caseId, CheckpointTypeId = checkpointType.Id, CreatedBy = audit.Clone() };
            var version = new DbCaseData { CaseId = caseId, CreatedBy = audit.Clone(), Data = System.Text.Json.Nodes.JsonNode.Parse("{}") };
            db.CaseTypes.Add(caseType);
            db.CheckpointTypes.Add(checkpointType);
            db.Cases.Add(new DbCase { Id = caseId, CaseTypeId = caseType.Id, Channel = "web", Draft = true, CreatedBy = audit, Owner = new ContactMeta { UserId = "owner" }, CheckpointId = checkpoint.Id, DataId = version.Id });
            db.Cases.Add(new DbCase { Id = otherCaseId, CaseTypeId = caseType.Id, Channel = "web", CreatedBy = audit.Clone(), Owner = new ContactMeta { UserId = "owner" } });
            db.Checkpoints.Add(checkpoint);
            db.CaseData.Add(version);
            var attachment = new DbAttachment(caseId);
            using var stream = new MemoryStream(content);
            attachment.PopulateFrom("document.txt", stream, saveData: !fileServiceEnabled);
            if (fileServiceEnabled) {
                await scope.ServiceProvider.GetRequiredKeyedService<IFileService>(CasesCoreConstants.FileServiceKey).SaveAsync(attachment.GetPath(), stream, attachment.ContentType);
            }
            var otherAttachment = new DbAttachment(otherCaseId) { Name = "other.txt", FileExtension = ".txt", ContentType = "text/plain", Data = content };
            attachmentId = attachment.Id;
            otherAttachmentId = otherAttachment.Id;
            db.Attachments.AddRange(attachment, otherAttachment);
            await db.SaveChangesAsync();
        }

        using var client = host.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "owner");
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
            var attachment = Assert.Single(body.GetProperty("attachments").EnumerateArray());
            Assert.Equal("document.txt", attachment.GetProperty("fileName").GetString());
            if (query.EndsWith("false")) {
                Assert.True(!attachment.TryGetProperty("data", out var data) || data.ValueKind == JsonValueKind.Null);
            } else {
                Assert.Equal(content, attachment.GetProperty("data").GetBytesFromBase64());
            }
        }
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
