using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Indice.Events;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Localization;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Features.Cases.Server;
using Indice.Features.Cases.Server.Authorization;
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

public class MyCaseTimelineTests
{
    private static readonly Guid CaseId = Guid.Parse("e5ba045e-c0d5-4db4-b671-275bcb3855ed");
    private static readonly DateTimeOffset Time = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static MyCaseService CreateService(CasesDbContext db) => new(
        db, Options.Create(new CasesOptions { CustomerTimelineStaffDisplayName = "Support" }),
        Substitute.For<IPlatformEventService>(), Substitute.For<IMyCaseMessageService>(),
        Substitute.For<IJsonTranslationService>(), new CaseSharedResourceService(Substitute.For<IStringLocalizerFactory>()));

    private static async Task Seed(CasesDbContext db, bool draft = false) {
        var type = new DbCaseType { Id = Guid.NewGuid(), Code = "T", Title = "Type", DataSchema = "{}" };
        var publicType = new DbCheckpointType {
            Id = Guid.NewGuid(), CaseTypeId = type.Id, Code = "PUBLIC", Title = "Original", Description = "Original description",
            Translations = new TranslationDictionary<CheckpointTypeTranslation> {
                ["el"] = new() { Title = "Translated", Description = "Translated description" }
            }
        };
        var privateType = new DbCheckpointType { Id = Guid.NewGuid(), CaseTypeId = type.Id, Code = "PRIVATE", Private = true };
        var owner = new AuditMeta { Id = "owner", Name = "Owner", Email = "owner@example.test", When = Time };
        var staff = new AuditMeta { Id = "staff", Name = "Staff", Email = "staff@example.test", When = Time.AddMinutes(1) };
        db.CaseTypes.Add(type);
        db.CheckpointTypes.AddRange(publicType, privateType);
        db.Cases.Add(new DbCase { Id = CaseId, CaseTypeId = type.Id, Channel = "web", Draft = draft, CreatedBy = owner, Owner = new ContactMeta { UserId = "owner" } });
        db.Checkpoints.AddRange(
            new DbCheckpoint { Id = Guid.NewGuid(), CaseId = CaseId, CheckpointTypeId = publicType.Id, CreatedBy = staff },
            new DbCheckpoint { Id = Guid.NewGuid(), CaseId = CaseId, CheckpointTypeId = privateType.Id, CreatedBy = staff.Clone() });
        var attachment = new DbAttachment(CaseId) { Name = "file.pdf", FileExtension = ".pdf", ContentType = "application/pdf", ContentLength = 123 };
        db.Attachments.Add(attachment);
        var original = new DbComment { CaseId = CaseId, Text = "Question", IsCustomer = true, CreatedBy = owner.Clone(), Attachment = attachment };
        var hidden = new DbComment { CaseId = CaseId, Text = "Secret", Private = true, CreatedBy = staff.Clone() };
        db.Comments.AddRange(original, hidden,
            new DbComment { CaseId = CaseId, Text = "Answer", ReplyToCommentId = original.Id, CreatedBy = new AuditMeta { Id = "staff", Name = "Staff", Email = "staff@example.test", When = Time.AddMinutes(2) } },
            new DbComment { CaseId = CaseId, Text = "Public reply to private", ReplyToCommentId = hidden.Id, CreatedBy = staff.Clone() });
        await db.SaveChangesAsync();
    }

    [Theory]
    [InlineData(TimelineFilter.All, 3, 1)]
    [InlineData(TimelineFilter.Comments, 3, 0)]
    [InlineData(TimelineFilter.Checkpoints, 0, 1)]
    public async Task Filters_public_entries_and_keeps_replies(TimelineFilter filter, int commentCount, int checkpointCount) {
        await using var db = new CasesDbContext(new DbContextOptionsBuilder<CasesDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        await Seed(db);
        var previousCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("el-GR");
        try {
            var timeline = await CreateService(db).GetTimeline(CaseId, new UserActor { Id = "owner" }, filter);
            Assert.NotNull(timeline);
            Assert.Equal(commentCount, timeline.Count(x => !x.IsCheckpoint));
            Assert.Equal(checkpointCount, timeline.Count(x => x.IsCheckpoint));
            Assert.Equal(timeline.OrderByDescending(x => x.Timestamp), timeline);
            Assert.DoesNotContain(timeline, x => x.Comment?.Text == "Secret");
            if (commentCount > 0) {
                var answer = Assert.Single(timeline, x => x.Comment?.Text == "Answer");
                Assert.Equal("Question", answer.Comment!.ReplyToComment!.Text);
                Assert.Null(answer.Comment.ReplyToComment.ReplyToComment);
                Assert.Equal("file.pdf", answer.Comment.ReplyToComment.Attachment!.Label);
                Assert.Equal("application/pdf", answer.Comment.ReplyToComment.Attachment.ContentType);
                Assert.Equal(123, answer.Comment.ReplyToComment.Attachment.Size);
                Assert.Equal("Support", answer.CreatedBy.Name);
                Assert.Null(answer.CreatedBy.Id);
                Assert.Null(answer.CreatedBy.Email);
                Assert.Null(Assert.Single(timeline, x => x.Comment?.Text == "Public reply to private").Comment!.ReplyToComment);
                Assert.Equal("owner", Assert.Single(timeline, x => x.Comment?.Text == "Question").CreatedBy.Id);
            }
            if (checkpointCount > 0) {
                var checkpoint = Assert.Single(timeline, x => x.IsCheckpoint).Checkpoint!;
                Assert.Equal("Translated", checkpoint.CheckpointType.Title);
                Assert.Equal("Translated description", checkpoint.CheckpointType.Description);
                var checkpointJson = JsonSerializer.SerializeToElement(checkpoint, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                Assert.False(checkpointJson.TryGetProperty("id", out _));
                var json = JsonSerializer.SerializeToElement(checkpoint.CheckpointType, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                Assert.False(json.TryGetProperty("id", out _));
                Assert.False(json.TryGetProperty("code", out _));
                Assert.False(json.TryGetProperty("status", out _));
                Assert.False(json.TryGetProperty("private", out _));
                Assert.True(json.TryGetProperty("translations", out _));
            }
        } finally {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public async Task Draft_is_empty_and_unknown_case_returns_null() {
        await using var db = new CasesDbContext(new DbContextOptionsBuilder<CasesDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        await Seed(db, draft: true);
        var service = CreateService(db);
        var user = new UserActor { Id = "owner" };
        Assert.Empty((await service.GetTimeline(CaseId, user, TimelineFilter.Comments))!);
        Assert.Null(await service.GetTimeline(Guid.NewGuid(), user));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.GetTimeline(CaseId, user, (TimelineFilter)999));
    }

    [Theory]
    [InlineData("", 4)]
    [InlineData("?filter=All", 4)]
    [InlineData("?filter=Comments", 3)]
    [InlineData("?filter=Checkpoints", 1)]
    public async Task Endpoint_binds_filter_and_enforces_ownership(string query, int count) {
        var database = Guid.NewGuid().ToString();
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
            });
            builder.Configure(app => {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints => endpoints.MapMyCases());
            });
        }).StartAsync();
        using (var scope = host.Services.CreateScope()) {
            await Seed(scope.ServiceProvider.GetRequiredService<CasesDbContext>());
        }
        using var client = host.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "owner");
        var response = await client.GetAsync($"/my/cases/{CaseId}/timeline{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(count, result.GetArrayLength());
        var invalid = await client.GetAsync($"/my/cases/{CaseId}/timeline?filter=999");
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        client.DefaultRequestHeaders.Remove("X-Test-User");
        client.DefaultRequestHeaders.Add("X-Test-User", "other");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/my/cases/{CaseId}/timeline{query}")).StatusCode);
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
