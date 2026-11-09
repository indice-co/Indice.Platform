using System.Security.Claims;
using Indice.Events;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Data.Stores;
using Indice.Features.Identity.Core.Totp;
using Indice.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Polly;
using Xunit;

namespace Indice.Features.Identity.Tests;

public class EmailChangeTests : IAsyncLifetime
{
    private readonly IHost _host;

    public EmailChangeTests() {
        _host = new HostBuilder().ConfigureWebHost(builder => {
        builder.UseTestServer();
        builder.ConfigureAppConfiguration(builder => {
            builder.AddInMemoryCollection(new Dictionary<string, string?> {
                ["IdentityOptions:User:EmailAsUserName"] = "true"
            });
        });
        builder.ConfigureServices((ctx, services) => {
            services.TryAddTransient<IPlatformEventService, DefaultPlatformEventService>();
            services.AddTransient<IUserRequirementProvider<User>, UserRequirementProviderNoOp>();
            services.AddTotpServiceFactory(ctx.Configuration)
                    .AddSmsServiceNoop()
                    .AddPushNotificationServiceNoop()
                    .AddLocalization()
                    .AddDistributedMemoryCache()
                    .AddDbContext<ExtendedIdentityDbContext<User, Role>>(builder => builder.UseInMemoryDatabase(Guid.NewGuid().ToString()))
                    .AddIdentity<User, Role>()
                    .AddDefaultTokenProviders()
                    .AddExtendedUserManager()
                    .AddExtendedSignInManager()
                    .AddEntityFrameworkStores<ExtendedIdentityDbContext<User, Role>>()
                    .AddUserStore<ExtendedUserStore<ExtendedIdentityDbContext<User, Role>, User, Role>>()
                    .AddExtendedPhoneNumberTokenProvider(ctx.Configuration)
                    .AddIdentityMessageDescriber<IdentityMessageDescriber>();
        });
        builder.Configure(app => { });
        }).Start();
        TestServer = _host.GetTestServer();
    }

    public TestServer TestServer { get; }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() {
        try {
            await _host.StopAsync();
        } finally {
            _host.Dispose();
        }
    }

    [Fact]
    public async Task ChangeUserEmail() {
        var random = new Random(Guid.NewGuid().GetHashCode()).Next();
        var email = $"dev_{random}@indice.gr";
        var updatedEmail = $"dev_{random}_2@indice.gr";
        var user = new User {
            CreateDate = DateTimeOffset.UtcNow,
            Email = email,
            Id = Guid.NewGuid().ToString(),
            SecurityStamp = Guid.NewGuid().ToString(),
            UserName = email
        };
        var userManager = TestServer.Services.GetRequiredService<ExtendedUserManager<User>>();
        // Create a new user.
        var identityResult = await userManager.CreateAsync(user);
        Assert.True(identityResult.Succeeded, "User creation failed");
        var token = await userManager.GenerateChangeEmailTokenAsync(user, updatedEmail);
        var result = await userManager.ChangeEmailAsync(user, updatedEmail, token);
        Assert.True(result.Succeeded, "User email change failed");
    }
}
