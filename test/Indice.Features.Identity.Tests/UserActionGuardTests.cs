using Indice.Events;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Data.Stores;
using Indice.Features.Identity.Core.Guards;
using Indice.Features.Identity.Core.Totp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Indice.Features.Identity.Tests;

public class UserActionGuardTests
{
    [Fact]
    public async Task RecordAttemptAsync_Is_Purpose_Scoped_And_Blocks_By_Configured_Limit() {
        var services = CreateServiceCollection(new Dictionary<string, string?> {
            [$"{UserActionGuardOptions.Name}:{nameof(UserActionGuardOptions.MaxAttempts)}"] = "3",
            [$"{UserActionGuardOptions.Name}:{nameof(UserActionGuardOptions.Window)}"] = "1.00:00:00"
        });

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExtendedIdentityDbContext<User, Role>>();
        var guard = scope.ServiceProvider.GetRequiredService<IUserActionGuard>();

        var user = new User("alice@example.com") {
            Email = "alice@example.com",
            CreateDate = DateTimeOffset.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var keyA = "Sms:ChangePhoneNumber";
        var keyB = "Sms:StrongCustomerAuthentication";

        Assert.False(await guard.IsBlockedAsync(user.Id, keyA));
        Assert.Equal(1, await guard.RecordAttemptAsync(user.Id, keyA));
        Assert.Equal(2, await guard.RecordAttemptAsync(user.Id, keyA));
        Assert.False(await guard.IsBlockedAsync(user.Id, keyA));

        Assert.Equal(1, await guard.RecordAttemptAsync(user.Id, keyB));
        Assert.False(await guard.IsBlockedAsync(user.Id, keyB));

        Assert.Equal(3, await guard.RecordAttemptAsync(user.Id, keyA));
        Assert.True(await guard.IsBlockedAsync(user.Id, keyA));
    }

    [Fact]
    public async Task RecordAttemptAsync_Resets_Count_When_WindowEnd_Has_Expired() {
        var services = CreateServiceCollection(new Dictionary<string, string?> {
            [$"{UserActionGuardOptions.Name}:{nameof(UserActionGuardOptions.MaxAttempts)}"] = "5",
            [$"{UserActionGuardOptions.Name}:{nameof(UserActionGuardOptions.Window)}"] = "1.00:00:00"
        });

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExtendedIdentityDbContext<User, Role>>();
        var guard = scope.ServiceProvider.GetRequiredService<IUserActionGuard>();

        var user = new User("bob@example.com") {
            Email = "bob@example.com",
            CreateDate = DateTimeOffset.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        db.Users.Add(user);
        db.UserActionAttempts.Add(new UserActionAttempt {
            UserId = user.Id,
            PurposeKey = "Sms:ChangePhoneNumber",
            Count = 4,
            WindowEnd = DateTimeOffset.UtcNow.AddMinutes(-1),
            LastAttemptDate = DateTimeOffset.UtcNow.AddHours(-1)
        });
        await db.SaveChangesAsync();

        var count = await guard.RecordAttemptAsync(user.Id, "Sms:ChangePhoneNumber");

        Assert.Equal(1, count);
        var row = await db.UserActionAttempts.SingleAsync(x => x.UserId == user.Id && x.PurposeKey == "Sms:ChangePhoneNumber");
        Assert.Equal(1, row.Count);
        Assert.True(row.WindowEnd > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task TotpService_Send_Is_Blocked_By_UserActionGuard_Limit() {
        var builder = new WebHostBuilder();
        builder.ConfigureAppConfiguration(configBuilder => {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?> {
                [$"{UserActionGuardOptions.Name}:{nameof(UserActionGuardOptions.MaxAttempts)}"] = "1",
                [$"{UserActionGuardOptions.Name}:{nameof(UserActionGuardOptions.Window)}"] = "1.00:00:00"
            });
        });

        builder.ConfigureServices(services => {
            services.TryAddTransient<IPlatformEventService, DefaultPlatformEventService>();
            var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            services.AddTransient<IUserRequirementProvider<User>, UserRequirementProviderNoOp>();
            services.AddTotpServiceFactory(configuration)
                    .AddUserActionGuard(configuration)
                    .AddSmsServiceNoop()
                    .AddEmailServiceNoop()
                    .AddPushNotificationServiceNoop()
                    .AddLocalization()
                    .AddSingleton<IDistributedCache, BlackholeDistributedCache>()
                    .AddDbContext<ExtendedIdentityDbContext<User, Role>>(dbBuilder => dbBuilder.UseInMemoryDatabase(Guid.NewGuid().ToString()))
                    .AddIdentity<User, Role>()
                    .AddExtendedUserManager()
                    .AddExtendedSignInManager()
                    .AddEntityFrameworkStores<ExtendedIdentityDbContext<User, Role>>()
                    .AddUserStore<ExtendedUserStore<ExtendedIdentityDbContext<User, Role>, User, Role>>()
                    .AddExtendedPhoneNumberTokenProvider(configuration)
                    .AddExtendedEmailTokenProvider(configuration)
                    .AddIdentityMessageDescriber<IdentityMessageDescriber>();
        });

        builder.Configure(_ => { });
        using var server = new TestServer(builder);

        var userManager = server.Services.GetRequiredService<ExtendedUserManager<User>>();
        var user = new User {
            CreateDate = DateTimeOffset.UtcNow,
            Email = "totp@example.com",
            Id = Guid.NewGuid().ToString(),
            PhoneNumber = "+306991234567",
            SecurityStamp = Guid.NewGuid().ToString(),
            UserName = "totp@example.com"
        };

        var createResult = await userManager.CreateAsync(user);
        Assert.True(createResult.Succeeded);

        var totpService = server.Services.GetRequiredService<TotpServiceFactory>().Create<User>();
        var purpose = "DailyGuardTest";

        var first = await totpService.SendAsync(totp => totp
            .ToUser(user)
            .WithMessage("Your one-time code is {0}")
            .UsingSms()
            .WithSubject("OTP")
            .WithPurpose(purpose));

        var second = await totpService.SendAsync(totp => totp
            .ToUser(user)
            .WithMessage("Your one-time code is {0}")
            .UsingSms()
            .WithSubject("OTP")
            .WithPurpose(purpose));

        Assert.True(first.Success);
        Assert.True(second.IsRateLimited);
        Assert.Contains("daily OTP request limit", second.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static ServiceCollection CreateServiceCollection(Dictionary<string, string?> settings) {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddDbContext<ExtendedIdentityDbContext<User, Role>>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddUserActionGuard(configuration);
        return services;
    }

    private sealed class BlackholeDistributedCache : IDistributedCache
    {
        public byte[]? Get(string key) => null;
        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult<byte[]?>(null);
        public void Refresh(string key) { }
        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;
        public void Remove(string key) { }
        public Task RemoveAsync(string key, CancellationToken token = default) => Task.CompletedTask;
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) { }
        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) => Task.CompletedTask;
    }

}
