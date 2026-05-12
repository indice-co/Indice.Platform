using HandlebarsDotNet;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Rendering;
using Indice.Features.Messages.Core.Services;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Indice.Features.Messages.Tests;

public class DbBackedPartialTemplateResolverTests : IAsyncLifetime
{
    public DbBackedPartialTemplateResolverTests() {
        var dbName = $"PartialResolverTests_{Guid.NewGuid()}";
        var services = new ServiceCollection()
            .AddLogging()
            .AddDbContext<CampaignsDbContext>(b => b.UseInMemoryDatabase(dbName), ServiceLifetime.Singleton)
            .AddTransient<ITemplateService, TemplateService>()
            .AddTransient<IPartialTemplateResolverFactory, DbBackedPartialTemplateResolverFactory>()
            .AddTransient(_ => new DatabaseSchemaNameResolver("cmp"))
            .AddTransient<IUserNameAccessor, UserNameAccessorNoOp>()
            .AddTransient<UserNameAccessorAggregate>();
        ServiceProvider = services.BuildServiceProvider();
    }

    private sealed class UserNameAccessorNoOp : IUserNameAccessor
    {
        public int Priority => 0;
        public string Resolve() => "test";
    }

    public ServiceProvider ServiceProvider { get; }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() {
        var db = ServiceProvider.GetRequiredService<CampaignsDbContext>();
        await db.Database.EnsureDeletedAsync();
        await ServiceProvider.DisposeAsync();
    }

    private async Task SeedTemplate(string alias, TemplateType type, params (MessageChannelKind channel, string body)[] content) {
        var svc = ServiceProvider.GetRequiredService<ITemplateService>();
        await svc.Create(new CreateTemplateRequest {
            Name = alias,
            Alias = alias,
            Type = type,
            Content = new MessageContentDictionary(content.ToDictionary(
                c => c.channel,
                c => new MessageContent(alias, c.body)))
        });
    }

    private IHandlebars CreateHandlebarsFor(string channel) {
        var factory = ServiceProvider.GetRequiredService<IPartialTemplateResolverFactory>();
        var hb = Handlebars.Create();
        hb.Configuration.PartialTemplateResolver = factory.Create(channel);
        return hb;
    }

    [Fact]
    public async Task Partial_HeaderAndFooter_WrapBody() {
        await SeedTemplate("header", TemplateType.Partial, (MessageChannelKind.Email, "=== H ==="));
        await SeedTemplate("footer", TemplateType.Partial, (MessageChannelKind.Email, "=== F ==="));

        var hb = CreateHandlebarsFor(MessageChannelKind.Email.ToString());
        var output = hb.Compile("{{> header}}{{data.body}}{{> footer}}")(new { data = new { body = "BODY" } });

        Assert.Equal("=== H ===BODY=== F ===", output);
    }

    [Fact]
    public async Task Layout_RendersBodyPartial() {
        await SeedTemplate("master", TemplateType.Layout, (MessageChannelKind.Email, "<html>{{> body}}</html>"));
        await SeedTemplate("body", TemplateType.Partial, (MessageChannelKind.Email, "Inner"));

        var hb = CreateHandlebarsFor(MessageChannelKind.Email.ToString());
        var output = hb.Compile("{{> master}}")(new { });

        Assert.Equal("<html>Inner</html>", output);
    }

    [Fact]
    public async Task Layout_TypeIsAccepted_LikePartial() {
        await SeedTemplate("wrap", TemplateType.Layout, (MessageChannelKind.Email, "[wrap]"));

        var hb = CreateHandlebarsFor(MessageChannelKind.Email.ToString());
        var output = hb.Compile("{{> wrap}}")(new { });

        Assert.Equal("[wrap]", output);
    }

    [Fact]
    public async Task Full_TypeIsRejected_RegistersEmpty() {
        await SeedTemplate("not-a-partial", TemplateType.Full, (MessageChannelKind.Email, "NOPE"));

        var hb = CreateHandlebarsFor(MessageChannelKind.Email.ToString());
        var output = hb.Compile("X{{> not-a-partial}}Y")(new { });

        Assert.Equal("XY", output);
    }

    [Fact]
    public void MissingAlias_RegistersEmpty() {
        var hb = CreateHandlebarsFor(MessageChannelKind.Email.ToString());
        var output = hb.Compile("X{{> ghost}}Y")(new { });

        Assert.Equal("XY", output);
    }

    [Fact]
    public async Task WrongChannel_RegistersEmpty() {
        await SeedTemplate("sms-only", TemplateType.Partial, (MessageChannelKind.SMS, "S"));

        var hb = CreateHandlebarsFor(MessageChannelKind.Email.ToString());
        var output = hb.Compile("X{{> sms-only}}Y")(new { });

        Assert.Equal("XY", output);
    }

    [Fact]
    public async Task EmptyBody_RegistersEmpty() {
        await SeedTemplate("blank", TemplateType.Partial, (MessageChannelKind.Email, string.Empty));

        var hb = CreateHandlebarsFor(MessageChannelKind.Email.ToString());
        var output = hb.Compile("X{{> blank}}Y")(new { });

        Assert.Equal("XY", output);
    }

    [Fact]
    public async Task Partial_ContainsHandlebarsExpressions() {
        await SeedTemplate("greet", TemplateType.Partial, (MessageChannelKind.Email, "Hi {{data.name}}"));

        var hb = CreateHandlebarsFor(MessageChannelKind.Email.ToString());
        var output = hb.Compile("{{> greet}}!")(new { data = new { name = "Alice" } });

        Assert.Equal("Hi Alice!", output);
    }

    [Fact]
    public async Task DifferentChannels_ResolveDifferentBodies() {
        await SeedTemplate("multi", TemplateType.Partial,
            (MessageChannelKind.Email, "E"),
            (MessageChannelKind.SMS, "S"));

        var emailOutput = CreateHandlebarsFor(MessageChannelKind.Email.ToString()).Compile("{{> multi}}")(new { });
        var smsOutput = CreateHandlebarsFor(MessageChannelKind.SMS.ToString()).Compile("{{> multi}}")(new { });

        Assert.Equal("E", emailOutput);
        Assert.Equal("S", smsOutput);
    }

    [Fact]
    public async Task PartialBlock_LayoutRendersInnerContentViaPartialBlock() {
        await SeedTemplate("layout", TemplateType.Layout, (MessageChannelKind.Email, "<html><body>{{> @partial-block}}</body></html>"));

        var hb = CreateHandlebarsFor(MessageChannelKind.Email.ToString());
        var output = hb.Compile("{{#> layout}}<p>Hello {{data.name}}</p>{{/layout}}")(new { data = new { name = "Alice" } });

        Assert.Equal("<html><body><p>Hello Alice</p></body></html>", output);
    }

    [Fact]
    public async Task NestedPartials_LayoutReferencesPartials() {
        await SeedTemplate("layout", TemplateType.Layout, (MessageChannelKind.Email, "<h>{{> hdr}}</h><b>{{> bdy}}</b>"));
        await SeedTemplate("hdr", TemplateType.Partial, (MessageChannelKind.Email, "HEADER"));
        await SeedTemplate("bdy", TemplateType.Partial, (MessageChannelKind.Email, "BODY"));

        var hb = CreateHandlebarsFor(MessageChannelKind.Email.ToString());
        var output = hb.Compile("{{> layout}}")(new { });

        Assert.Equal("<h>HEADER</h><b>BODY</b>", output);
    }
}
