using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
#if NET9_0_OR_GREATER
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
#else
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
#endif
using Indice.AspNetCore.Authorization;
using Indice.Events;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Data.Stores;
using Indice.Features.Identity.Core.Events;
using Indice.Features.Identity.Core.Events.Models;
using Indice.Features.Identity.Server;
using Indice.Features.Identity.Server.Manager.Models;
using Indice.Features.Identity.Tests.Security;
using Indice.Security;
using Indice.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Indice.Features.Identity.Tests;

public class TemporaryPasswordTests : IAsyncLifetime
{
    private const string InitialPassword = "Xk9#mTq2!vLp";
    private const string NewPassword = "Zr4$hWn8@bQe";
    private readonly List<IHost> _hosts = [];
    private readonly List<HttpClient> _httpClients = [];

    [Fact]
    public async Task MarkPasswordAsTemporary_FlagsPassword_And_SetsCutoffFromLifetime() {
        var (host, _) = CreateHost(new() { ["IdentityOptions:Password:TemporaryPasswordLifetime"] = "1.00:00:00" });
        using var scope = host.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var user = await CreateUserAsync(userManager, PasswordExpirationPolicy.Annually);

        var result = await userManager.MarkPasswordAsTemporaryAsync(user, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.True(user.PasswordIsTemporary);
        Assert.True(user.PasswordExpired);
        var cutoff = Assert.NotNull(user.PasswordExpirationDate);
        Assert.InRange(cutoff, DateTimeOffset.UtcNow.AddHours(23), DateTimeOffset.UtcNow.AddHours(25));
        Assert.False(user.HasExpiredTemporaryPassword());
        Assert.True(user.HasExpiredTemporaryPassword(DateTimeOffset.UtcNow.AddDays(2)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(PasswordExpirationPolicy.Monthly)]
    public async Task MarkPasswordAsTemporary_WithoutLifetime_UsesPolicyDate(PasswordExpirationPolicy? policy) {
        var (host, _) = CreateHost([]);
        using var scope = host.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var user = await CreateUserAsync(userManager, policy);
        var policyDate = user.PasswordExpirationDate;

        await userManager.MarkPasswordAsTemporaryAsync(user, TestContext.Current.CancellationToken);

        Assert.Null(userManager.TemporaryPasswordLifetime);
        Assert.True(user.PasswordIsTemporary);
        Assert.True(user.PasswordExpired);
        Assert.Equal(policyDate, user.PasswordExpirationDate);
        Assert.Equal(policy.HasValue, user.HasExpiredTemporaryPassword(DateTimeOffset.UtcNow.AddYears(1)));
    }

    [Fact]
    public async Task TemporaryPasswordCutoff_IsNotMovedByUpdates() {
        var (host, _) = CreateHost(new() { ["IdentityOptions:Password:TemporaryPasswordLifetime"] = "1.00:00:00" });
        string userId;
        DateTimeOffset? cutoff;
        using (var scope = host.Services.CreateScope()) {
            var userManager = scope.ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
            var user = await CreateUserAsync(userManager);
            userId = user.Id;
            await userManager.MarkPasswordAsTemporaryAsync(user, TestContext.Current.CancellationToken);
            cutoff = user.PasswordExpirationDate;
            await userManager.SetPasswordExpirationPolicyAsync(user, PasswordExpirationPolicy.Annually, TestContext.Current.CancellationToken);
            await userManager.SetLastSignInDateAsync(user, cancellationToken: TestContext.Current.CancellationToken);
            await userManager.UpdateAsync(user);
        }
        using (var scope = host.Services.CreateScope()) {
            var userManager = scope.ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
            var user = await userManager.FindByIdAsync(userId);
            Assert.NotNull(user);
            Assert.True(user.PasswordIsTemporary);
            Assert.True(user.PasswordExpired);
            Assert.Equal(cutoff, user.PasswordExpirationDate);
        }
    }

    [Fact]
    public async Task CheckPasswordSignIn_WithExpiredTemporaryPassword_ReturnsTemporaryPasswordExpired() {
        var (host, _) = CreateHost(new() { ["IdentityOptions:Password:TemporaryPasswordLifetime"] = "1.00:00:00" });
        using var scope = host.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var signInManager = scope.ServiceProvider.GetRequiredService<ExtendedSignInManager<User>>();
        var user = await CreateUserAsync(userManager);
        await userManager.MarkPasswordAsTemporaryAsync(user, TestContext.Current.CancellationToken);
        await ExpireTemporaryPasswordAsync(userManager, user);

        var result = await signInManager.CheckPasswordSignInAsync(user, InitialPassword, lockoutOnFailure: true);

        Assert.False(result.Succeeded);
        Assert.True(result.IsTemporaryPasswordExpired());
    }

    [Fact]
    public async Task CheckPasswordSignIn_WithExpiredTemporaryPassword_AndWrongPassword_ReturnsPlainFailure() {
        var (host, _) = CreateHost(new() { ["IdentityOptions:Password:TemporaryPasswordLifetime"] = "1.00:00:00" });
        using var scope = host.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var signInManager = scope.ServiceProvider.GetRequiredService<ExtendedSignInManager<User>>();
        var user = await CreateUserAsync(userManager);
        await userManager.MarkPasswordAsTemporaryAsync(user, TestContext.Current.CancellationToken);
        await ExpireTemporaryPasswordAsync(userManager, user);

        var result = await signInManager.CheckPasswordSignInAsync(user, "wrong-password", lockoutOnFailure: false);

        Assert.False(result.Succeeded);
        Assert.False(result.IsTemporaryPasswordExpired());
    }

    [Fact]
    public async Task CheckPasswordSignIn_WithRegularExpiredPassword_IsNotRefused() {
        var (host, _) = CreateHost(new() { ["IdentityOptions:Password:TemporaryPasswordLifetime"] = "1.00:00:00" });
        using var scope = host.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var signInManager = scope.ServiceProvider.GetRequiredService<ExtendedSignInManager<User>>();
        signInManager.Context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        var user = await CreateUserAsync(userManager, PasswordExpirationPolicy.Monthly);
        user.LastPasswordChangeDate = DateTimeOffset.UtcNow.AddDays(-60);
        await userManager.UpdateAsync(user);
        Assert.True(user.HasExpiredPassword());

        var result = await signInManager.CheckPasswordSignInAsync(user, InitialPassword, lockoutOnFailure: true);

        Assert.True(result.Succeeded);
        Assert.False(result.IsTemporaryPasswordExpired());
    }

    [Fact]
    public async Task ChangePassword_ClearsTemporaryPassword() {
        var (host, _) = CreateHost(new() { ["IdentityOptions:Password:TemporaryPasswordLifetime"] = "1.00:00:00" });
        string userId;
        using (var scope = host.Services.CreateScope()) {
            var userManager = scope.ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
            var user = await CreateUserAsync(userManager);
            userId = user.Id;
            await userManager.MarkPasswordAsTemporaryAsync(user, TestContext.Current.CancellationToken);
            var result = await userManager.ChangePasswordAsync(user, InitialPassword, NewPassword);
            Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(x => x.Description)));
        }
        using (var scope = host.Services.CreateScope()) {
            var userManager = scope.ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
            var user = await userManager.FindByIdAsync(userId);
            Assert.NotNull(user);
            Assert.False(user.PasswordIsTemporary);
            Assert.False(user.PasswordExpired);
            Assert.Null(user.PasswordExpirationDate);
        }
    }

    [Fact]
    public async Task ResetPasswordWithToken_ClearsTemporaryPassword() {
        var (host, _) = CreateHost(new() { ["IdentityOptions:Password:TemporaryPasswordLifetime"] = "1.00:00:00" });
        string userId;
        using (var scope = host.Services.CreateScope()) {
            var userManager = scope.ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
            var user = await CreateUserAsync(userManager, PasswordExpirationPolicy.Monthly);
            userId = user.Id;
            await userManager.MarkPasswordAsTemporaryAsync(user, TestContext.Current.CancellationToken);
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, token, NewPassword);
            Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(x => x.Description)));
        }
        using (var scope = host.Services.CreateScope()) {
            var userManager = scope.ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
            var user = await userManager.FindByIdAsync(userId);
            Assert.NotNull(user);
            Assert.False(user.PasswordIsTemporary);
            Assert.False(user.PasswordExpired);
            var expirationDate = Assert.NotNull(user.PasswordExpirationDate);
            Assert.InRange(expirationDate, DateTimeOffset.UtcNow.AddDays(29), DateTimeOffset.UtcNow.AddDays(31));
        }
    }

    [Fact]
    public async Task SetPasswordExpiredFalse_TurnsTemporaryPasswordIntoRegularOne() {
        var (host, _) = CreateHost(new() { ["IdentityOptions:Password:TemporaryPasswordLifetime"] = "1.00:00:00" });
        using var scope = host.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var user = await CreateUserAsync(userManager);
        await userManager.MarkPasswordAsTemporaryAsync(user, TestContext.Current.CancellationToken);

        await userManager.SetPasswordExpiredAsync(user, false, TestContext.Current.CancellationToken);

        Assert.False(user.PasswordIsTemporary);
        Assert.False(user.PasswordExpired);
        Assert.Null(user.PasswordExpirationDate);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task ChangePassword_RevokesPersistedGrants_OnlyWhenEnabled(bool revokeGrants, bool expectGrantsToRemain) {
        var (host, _) = CreateHost(new() { ["IdentityOptions:Password:RevokeGrantsOnPasswordChange"] = revokeGrants.ToString() });
        using var scope = host.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var grantStore = scope.ServiceProvider.GetRequiredService<IPersistedGrantStore>();
        var user = await CreateUserAsync(userManager);
        await grantStore.StoreAsync(new PersistedGrant {
            Key = Guid.NewGuid().ToString(),
            Type = "refresh_token",
            SubjectId = user.Id,
            ClientId = "client",
            CreationTime = DateTime.UtcNow,
            Data = "{}"
        });

        var result = await userManager.ChangePasswordAsync(user, InitialPassword, NewPassword);

        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(x => x.Description)));
        var grants = await grantStore.GetAllAsync(new PersistedGrantFilter { SubjectId = user.Id });
        Assert.Equal(expectGrantsToRemain, grants.Any());
    }

    [Fact]
    public async Task ChangePassword_WhenGrantRevocationFails_PropagatesFailure() {
        var grantService = new Mock<IPersistedGrantService>();
        grantService.Setup(service => service.RemoveAllGrantsAsync(It.IsAny<string>()))
                    .Returns(Task.FromException(new InvalidOperationException("Grant revocation failed.")));
        var (host, _) = CreateHost(new() { ["IdentityOptions:Password:RevokeGrantsOnPasswordChange"] = "true" }, persistedGrantService: grantService.Object);
        using var scope = host.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var user = await CreateUserAsync(userManager);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => userManager.ChangePasswordAsync(user, InitialPassword, NewPassword));

        Assert.Equal("Grant revocation failed.", error.Message);
        grantService.Verify(service => service.RemoveAllGrantsAsync(user.Id), Times.Once);
    }

    [Fact]
    public async Task ChangePassword_WithBackgroundEventService_RetriesGrantRevocationFailure() {
        var attempts = 0;
        var revoked = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var grantService = new Mock<IPersistedGrantService>();
        grantService.Setup(service => service.RemoveAllGrantsAsync(It.IsAny<string>()))
                    .Returns(() => {
                        if (Interlocked.Increment(ref attempts) == 1) {
                            return Task.FromException(new InvalidOperationException("Temporary grant store failure."));
                        }
                        revoked.TrySetResult(true);
                        return Task.CompletedTask;
                    });
        var (host, _) = CreateHost(new() { ["IdentityOptions:Password:RevokeGrantsOnPasswordChange"] = "true" }, backgroundEvents: true, persistedGrantService: grantService.Object);
        using var scope = host.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var user = await CreateUserAsync(userManager);

        var result = await userManager.ChangePasswordAsync(user, InitialPassword, NewPassword);

        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(x => x.Description)));
        await revoked.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task SetPasswordApi_WithChangePasswordAfterFirstSignIn_MarksPasswordAsTemporary() {
        var (host, httpClient) = CreateHost(new() { ["IdentityOptions:Password:TemporaryPasswordLifetime"] = "1.00:00:00" });
        var userId = await CreateUserAsync(host);
        var saves = host.Services.GetRequiredService<UserSaveRecorder>();
        saves.Clear();

        var response = await httpClient.PutAsJsonAsync($"/api/users/{userId}/set-password", new SetPasswordRequest {
            Password = NewPassword,
            ChangePasswordAfterFirstSignIn = true
        }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        AssertStoredAsTemporary(saves.For(userId));
        var setEvent = Assert.Single(host.Services.GetRequiredService<EventRecorder>().Users<PasswordSetEvent>(userId));
        Assert.True(setEvent.PasswordIsTemporary);
        using var scope = host.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var user = await userManager.FindByIdAsync(userId);
        Assert.NotNull(user);
        Assert.True(user.PasswordIsTemporary);
        Assert.True(user.PasswordExpired);
        var cutoff = Assert.NotNull(user.PasswordExpirationDate);
        Assert.InRange(cutoff, DateTimeOffset.UtcNow.AddHours(23), DateTimeOffset.UtcNow.AddHours(25));
    }

    [Fact]
    public async Task CreateUserApi_WithChangePasswordAfterFirstSignIn_ReturnsTemporaryState() {
        var (host, httpClient) = CreateHost(new() { ["IdentityOptions:Password:TemporaryPasswordLifetime"] = "1.00:00:00" });
        var id = Guid.NewGuid().ToString();

        var response = await httpClient.PostAsJsonAsync("/api/users", new CreateUserRequest {
            Email = $"{id}@indice.gr",
            UserName = $"{id}@indice.gr",
            Password = InitialPassword,
            ChangePasswordAfterFirstSignIn = true
        }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<SingleUserInfo>(TestContext.Current.CancellationToken);
        Assert.NotNull(user);
        Assert.True(user.PasswordIsTemporary);
        var cutoff = Assert.NotNull(user.PasswordExpirationDate);
        Assert.InRange(cutoff, DateTimeOffset.UtcNow.AddHours(23), DateTimeOffset.UtcNow.AddHours(25));
        // The password is never stored, nor announced, as a regular one.
        AssertStoredAsTemporary(host.Services.GetRequiredService<UserSaveRecorder>().For(user.Id!));
        var createdEvent = Assert.Single(host.Services.GetRequiredService<EventRecorder>().Users<UserCreatedEvent>(user.Id!));
        Assert.True(createdEvent.PasswordIsTemporary);
        Assert.Equal(cutoff, createdEvent.PasswordExpirationDate);
    }

    [Fact]
    public async Task CreateUserApi_WithoutLifetime_UsesDefaultPolicyDateAsCutoff() {
        var (_, httpClient) = CreateHost(new() { ["IdentityOptions:Password:PasswordExpirationPolicy"] = nameof(PasswordExpirationPolicy.Monthly) });
        var id = Guid.NewGuid().ToString();

        var response = await httpClient.PostAsJsonAsync("/api/users", new CreateUserRequest {
            Email = $"{id}@indice.gr",
            UserName = $"{id}@indice.gr",
            Password = InitialPassword,
            ChangePasswordAfterFirstSignIn = true
        }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<SingleUserInfo>(TestContext.Current.CancellationToken);
        Assert.NotNull(user);
        Assert.True(user.PasswordIsTemporary);
        var cutoff = Assert.NotNull(user.PasswordExpirationDate);
        Assert.InRange(cutoff, DateTimeOffset.UtcNow.AddDays(27), DateTimeOffset.UtcNow.AddDays(32));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PasswordRehash_KeepsTemporaryPasswordState(bool expired) {
        var hasher = new RehashingPasswordHasher();
        var (host, _) = CreateHost(new() { ["IdentityOptions:Password:TemporaryPasswordLifetime"] = "1.00:00:00" }, hasher);
        string userId;
        string? passwordHash;
        DateTimeOffset? cutoff;
        using (var scope = host.Services.CreateScope()) {
            var userManager = scope.ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
            var signInManager = scope.ServiceProvider.GetRequiredService<ExtendedSignInManager<User>>();
            var user = await CreateUserAsync(userManager);
            userId = user.Id;
            await userManager.MarkPasswordAsTemporaryAsync(user, TestContext.Current.CancellationToken);
            if (expired) {
                await ExpireTemporaryPasswordAsync(userManager, user);
            }
            (passwordHash, cutoff) = (user.PasswordHash, user.PasswordExpirationDate);
            hasher.RehashNeeded = true;
            host.Services.GetRequiredService<UserSaveRecorder>().Clear();

            if (expired) {
                var result = await signInManager.CheckPasswordSignInAsync(user, InitialPassword, lockoutOnFailure: true);
                Assert.True(result.IsTemporaryPasswordExpired());
            } else {
                Assert.True(await userManager.CheckPasswordAsync(user, InitialPassword));
            }
        }
        using (var scope = host.Services.CreateScope()) {
            // A concurrent sign-in can only ever read the temporary state, since every write of the user kept it.
            AssertStoredAsTemporary(host.Services.GetRequiredService<UserSaveRecorder>().For(userId));
            var userManager = scope.ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
            var user = await userManager.FindByIdAsync(userId);
            Assert.NotNull(user);
            Assert.NotEqual(passwordHash, user.PasswordHash);
            Assert.True(user.PasswordIsTemporary);
            Assert.True(user.PasswordExpired);
            Assert.Equal(cutoff, user.PasswordExpirationDate);
        }
    }

    [Fact]
    public async Task ResetPasswordWithToken_ForLockedOutUser_RevokesPersistedGrants() {
        var (host, _) = CreateHost(new() { ["IdentityOptions:Password:RevokeGrantsOnPasswordChange"] = "true" });
        using var scope = host.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var grantStore = scope.ServiceProvider.GetRequiredService<IPersistedGrantStore>();
        var user = await CreateUserAsync(userManager);
        await StoreGrantAsync(grantStore, user.Id);
        await userManager.SetLockoutEnabledAsync(user, true);
        await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddHours(1));
        Assert.True(await userManager.IsLockedOutAsync(user));

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, NewPassword);

        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(x => x.Description)));
        Assert.False(await userManager.IsLockedOutAsync(user));
        Assert.Empty(await grantStore.GetAllAsync(new PersistedGrantFilter { SubjectId = user.Id }));
    }

    [Fact]
    public async Task ChangePassword_WithBackgroundEventService_RevokesPersistedGrants() {
        var (host, _) = CreateHost(new() { ["IdentityOptions:Password:RevokeGrantsOnPasswordChange"] = "true" }, backgroundEvents: true);
        using var scope = host.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var grantStore = scope.ServiceProvider.GetRequiredService<IPersistedGrantStore>();
        var user = await CreateUserAsync(userManager);
        await StoreGrantAsync(grantStore, user.Id);

        var result = await userManager.ChangePasswordAsync(user, InitialPassword, NewPassword);

        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(x => x.Description)));
        var timeout = DateTime.UtcNow.AddSeconds(10);
        while ((await grantStore.GetAllAsync(new PersistedGrantFilter { SubjectId = user.Id })).Any() && DateTime.UtcNow < timeout) {
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }
        Assert.Empty(await grantStore.GetAllAsync(new PersistedGrantFilter { SubjectId = user.Id }));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task SetPasswordApi_RespectsRequirePasswordChangeAfterAdminSet(bool requireChange, bool expectExpired) {
        var (host, httpClient) = CreateHost(new() { ["IdentityOptions:Password:RequirePasswordChangeAfterAdminSet"] = requireChange.ToString() });
        var userId = await CreateUserAsync(host);

        var response = await httpClient.PutAsJsonAsync($"/api/users/{userId}/set-password", new SetPasswordRequest {
            Password = NewPassword,
            ChangePasswordAfterFirstSignIn = false
        }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var scope = host.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var user = await userManager.FindByIdAsync(userId);
        Assert.NotNull(user);
        Assert.Equal(expectExpired, user.PasswordExpired);
        Assert.Equal(expectExpired, user.PasswordIsTemporary);
    }

    [Fact]
    public async Task SendPasswordResetApi_SendsEmailToUser() {
        var (host, httpClient) = CreateHost([]);
        var userId = await CreateUserAsync(host);
        var emailService = (FakeEmailService)host.Services.GetRequiredService<IEmailService>();

        var response = await httpClient.PostAsJsonAsync($"/api/users/{userId}/send-password-reset", new SendPasswordResetEmailRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var recipients = Assert.Single(emailService.SentTo);
        Assert.Equal($"{userId}@indice.gr", Assert.Single(recipients));
    }

    [Fact]
    public async Task SendPasswordResetApi_ForUnknownUser_ReturnsNotFound() {
        var (host, httpClient) = CreateHost([]);
        var emailService = (FakeEmailService)host.Services.GetRequiredService<IEmailService>();

        var response = await httpClient.PostAsJsonAsync($"/api/users/{Guid.NewGuid()}/send-password-reset", new SendPasswordResetEmailRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(emailService.SentTo);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() {
        foreach (var httpClient in _httpClients) {
            httpClient.Dispose();
        }
        foreach (var host in _hosts) {
            await host.StopAsync();
            host.Dispose();
        }
    }

    private static Task StoreGrantAsync(IPersistedGrantStore grantStore, string subjectId) => grantStore.StoreAsync(new PersistedGrant {
        Key = Guid.NewGuid().ToString(),
        Type = "refresh_token",
        SubjectId = subjectId,
        ClientId = "client",
        CreationTime = DateTime.UtcNow,
        Data = "{}"
    });

    private static async Task<User> CreateUserAsync(ExtendedUserManager<User> userManager, PasswordExpirationPolicy? policy = null) {
        var id = Guid.NewGuid().ToString();
        var user = new User($"{id}@indice.gr", id) {
            Email = $"{id}@indice.gr",
            EmailConfirmed = true,
            PasswordExpirationPolicy = policy
        };
        var result = await userManager.CreateAsync(user, InitialPassword);
        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(x => x.Description)));
        return user;
    }

    private static async Task<string> CreateUserAsync(IHost host) {
        using var scope = host.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
        return (await CreateUserAsync(userManager)).Id;
    }

    private static void AssertStoredAsTemporary(IReadOnlyList<bool> saves) {
        Assert.NotEmpty(saves);
        Assert.All(saves, passwordIsTemporary => Assert.True(passwordIsTemporary));
    }

    private static async Task ExpireTemporaryPasswordAsync(ExtendedUserManager<User> userManager, User user) {
        user.PasswordExpirationDate = DateTimeOffset.UtcNow.AddMinutes(-1);
        var result = await userManager.UpdateAsync(user);
        Assert.True(result.Succeeded);
        Assert.True(user.HasExpiredTemporaryPassword());
    }

    private (IHost Host, HttpClient HttpClient) CreateHost(Dictionary<string, string?> settings, IPasswordHasher<User>? passwordHasher = null, bool backgroundEvents = false, IPersistedGrantService? persistedGrantService = null) {
        var databaseName = $"TemporaryPasswordTests_{Guid.NewGuid()}";
        var host = new HostBuilder().ConfigureWebHost(builder => {
            builder.UseTestServer();
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(settings));
            builder.ConfigureServices((context, services) => {
                services.AddSingleton<UserSaveRecorder>();
                services.AddSingleton<EventRecorder>();
                services.AddPlatformEventHandler<UserCreatedEvent, RecordingEventHandler>();
                services.AddPlatformEventHandler<PasswordSetEvent, RecordingEventHandler>();
                services.AddDbContext<ExtendedIdentityDbContext<User, Role>>((sp, builder) => builder.UseInMemoryDatabase(databaseName).AddInterceptors(sp.GetRequiredService<UserSaveRecorder>()));
                services.AddDbContext<ExtendedConfigurationDbContext>(builder => builder.UseInMemoryDatabase(databaseName));
                services.AddTransient(sp => new ExtendedIdentityDbContextSeedOptions<User, Role> { InitialUsers = [], CustomRoles = [] });
                services.AddDataProtection();
                services.AddHttpContextAccessor();
                services.AddTransient<IUserRequirementProvider<User>, UserRequirementProviderNoOp>();
                services.AddIdentity<User, Role>()
                        .AddExtendedUserManager()
                        .AddUserStore<ExtendedUserStore<ExtendedIdentityDbContext<User, Role>, User, Role>>()
                        .AddExtendedSignInManager()
                        .AddEntityFrameworkStores<ExtendedIdentityDbContext<User, Role>>()
                        .AddDefaultTokenProviders()
                        .AddExtendedPhoneNumberTokenProvider(context.Configuration)
                        .AddIdentityMessageDescriber();
                services.AddIdentityServer()
                        .AddInMemoryIdentityResources([])
                        .AddInMemoryApiScopes([])
                        .AddInMemoryApiResources([])
                        .AddInMemoryClients([])
                        .AddAspNetIdentity<User>()
                        .AddInMemoryPersistedGrants();
                if (persistedGrantService is not null) {
                    services.AddSingleton(persistedGrantService);
                }
                services.AddSingleton<IEmailService, FakeEmailService>();
                if (backgroundEvents) {
                    services.AddBackgroundPlatformEventService();
                } else {
                    services.AddDefaultPlatformEventService();
                }
                if (passwordHasher is not null) {
                    services.AddSingleton(passwordHasher);
                }
                services.AddEndpointParameterFluentValidation();
                services.AddOutputCache();
                services.AddLogging();
                services.AddLocalization()
                        .AddActionRateLimiter(context.Configuration)
                        .AddRouting()
                        .AddAuthorization(authOptions =>
                            authOptions.AddPolicy(IdentityEndpoints.Policies.BeUsersWriter, policy => {
                                policy.AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme)
                                      .RequireAuthenticatedUser()
                                      .RequireAssertion(x => x.User.HasScope(IdentityEndpoints.SubScopes.Users) && x.User.CanReadUsers());
                            }))
                        .AddAuthentication(MockAuthenticationDefaults.AuthenticationScheme)
                        .AddJwtBearer(options => options.ForwardDefaultSelector = httpContext => MockAuthenticationDefaults.AuthenticationScheme)
                        .AddMock("IdentityServerApiAccessToken", "LocalApi", () => TestPrincipals.UserWriter);
            });
            builder.Configure(app => {
                app.UseAuthentication();
                app.UseRouting();
                app.UseAuthorization();
                app.UseOutputCache();
                app.UseEndpoints(routes => new IdentityServerEndpointRouteBuilder(routes).MapManageUsers());
            });
        }).Start();
        _hosts.Add(host);
        var httpClient = new HttpClient(host.GetTestServer().CreateHandler()) {
            BaseAddress = new Uri("https://server")
        };
        _httpClients.Add(httpClient);
        return (host, httpClient);
    }

    /// <summary>Records the <see cref="User.PasswordIsTemporary"/> value of every user written to the database.</summary>
    private sealed class UserSaveRecorder : SaveChangesInterceptor
    {
        private readonly ConcurrentQueue<(string UserId, bool PasswordIsTemporary)> _saves = new();

        public IReadOnlyList<bool> For(string userId) => _saves.Where(x => x.UserId == userId).Select(x => x.PasswordIsTemporary).ToList();

        public void Clear() => _saves.Clear();

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result) {
            Record(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) {
            Record(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void Record(DbContext? context) {
            foreach (var entry in context?.ChangeTracker.Entries<User>().Where(x => x.State is EntityState.Added or EntityState.Modified) ?? []) {
                _saves.Enqueue((entry.Entity.Id, entry.Entity.PasswordIsTemporary));
            }
        }
    }

    private sealed class EventRecorder
    {
        private readonly ConcurrentQueue<(Type EventType, UserEventContext User)> _events = new();

        public void Add(Type eventType, UserEventContext user) => _events.Enqueue((eventType, user));

        public IReadOnlyList<UserEventContext> Users<TEvent>(string userId) => _events.Where(x => x.EventType == typeof(TEvent) && x.User.Id == userId).Select(x => x.User).ToList();
    }

    private sealed class RecordingEventHandler(EventRecorder recorder) : IPlatformEventHandler<UserCreatedEvent>, IPlatformEventHandler<PasswordSetEvent>
    {
        Task IPlatformEventHandler<UserCreatedEvent>.Handle(UserCreatedEvent @event, PlatformEventArgs args) {
            recorder.Add(typeof(UserCreatedEvent), @event.User);
            return Task.CompletedTask;
        }

        Task IPlatformEventHandler<PasswordSetEvent>.Handle(PasswordSetEvent @event, PlatformEventArgs args) {
            recorder.Add(typeof(PasswordSetEvent), @event.User);
            return Task.CompletedTask;
        }
    }

    private sealed class RehashingPasswordHasher : PasswordHasher<User>
    {
        public bool RehashNeeded { get; set; }

        public override PasswordVerificationResult VerifyHashedPassword(User user, string hashedPassword, string providedPassword) {
            var result = base.VerifyHashedPassword(user, hashedPassword, providedPassword);
            return RehashNeeded && result == PasswordVerificationResult.Success ? PasswordVerificationResult.SuccessRehashNeeded : result;
        }
    }

    private sealed class FakeEmailService : IEmailService
    {
        public List<string[]> SentTo { get; } = [];
        public IHtmlRenderingEngine? HtmlRenderingEngine => null;
        public EmailProvider Provider => new("Fake", new EmailSender("noreply@indice.gr", "Indice"));

        public Task<SendReceipt> SendAsync(string[] recipients, string subject, string? body, EmailAttachment[]? attachments = null, EmailSender? from = null) {
            SentTo.Add(recipients);
            return Task.FromResult(new SendReceipt(Guid.NewGuid().ToString(), DateTimeOffset.UtcNow));
        }
    }
}
