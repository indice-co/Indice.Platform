using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Guards;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var keyA = "Sms:ChangePhoneNumber";
        var keyB = "Sms:StrongCustomerAuthentication";

        Assert.False(await guard.IsBlockedAsync(user.Id, keyA, TestContext.Current.CancellationToken));
        Assert.Equal(1, await guard.RecordAttemptAsync(user.Id, keyA, TestContext.Current.CancellationToken));
        Assert.Equal(2, await guard.RecordAttemptAsync(user.Id, keyA, TestContext.Current.CancellationToken ));
        Assert.False(await guard.IsBlockedAsync(user.Id, keyA, TestContext.Current.CancellationToken));

        Assert.Equal(1, await guard.RecordAttemptAsync(user.Id, keyB, TestContext.Current.CancellationToken));
        Assert.False(await guard.IsBlockedAsync(user.Id, keyB, TestContext.Current.CancellationToken));

        Assert.Equal(3, await guard.RecordAttemptAsync(user.Id, keyA, TestContext.Current.CancellationToken));
        Assert.True(await guard.IsBlockedAsync(user.Id, keyA, TestContext.Current.CancellationToken));
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
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var count = await guard.RecordAttemptAsync(user.Id, "Sms:ChangePhoneNumber", TestContext.Current.CancellationToken);

        Assert.Equal(1, count);
        var row = await db.UserActionAttempts.SingleAsync(x => x.UserId == user.Id && x.PurposeKey == "Sms:ChangePhoneNumber", TestContext.Current.CancellationToken);
        Assert.Equal(1, row.Count);
        Assert.True(row.WindowEnd > DateTimeOffset.UtcNow);
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
}
