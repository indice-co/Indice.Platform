using System.Security;
using Indice.Events;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Data.Stores;
using Indice.Features.Identity.Core.Events;
using Indice.Features.Identity.Core.Hubs;
using Indice.Features.Identity.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Indice.Features.Identity.Tests;

public class TwoFactorPreferenceTests : IAsyncLifetime
{
    private readonly ServiceProvider _rootProvider;
    private readonly AsyncServiceScope _scope;

    public TwoFactorPreferenceTests() {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["IdentityOptions:SignIn:Mfa:Policy"] = "Default"
        }).Build();
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection()
            .AddSingleton(configuration)
            .AddLogging();
        services.AddTransient<IUserRequirementProvider<User>, UserRequirementProviderNoOp>();
        services.AddTotpServiceFactory(configuration)
                .AddDefaultPlatformEventService()
                .AddSmsServiceNoop()
                .AddPushNotificationServiceNoop()
                .AddLocalization()
                .AddDistributedMemoryCache()
                .AddDbContext<ExtendedIdentityDbContext<User, Role>>(builder => builder.UseInMemoryDatabase(databaseName))
                .AddIdentity<User, Role>()
                .AddDefaultTokenProviders()
                .AddExtendedUserManager()
                .AddExtendedSignInManager()
                .AddEntityFrameworkStores<ExtendedIdentityDbContext<User, Role>>()
                .AddUserStore<ExtendedUserStore<ExtendedIdentityDbContext<User, Role>, User, Role>>()
                .AddIdentityMessageDescriber<IdentityMessageDescriber>();
        // The security notification handler needs IdentityServer host services.
        services.RemoveAll<IPlatformEventHandler<TwoFactorPreferenceChangedEvent>>();
        _rootProvider = services.BuildServiceProvider();
        _scope = _rootProvider.CreateAsyncScope();
    }

    private ExtendedUserManager<User> UserManager => _scope.ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();

    [Fact]
    public void SelectPreferred_Uses_Preference_Among_Methods_Of_The_Highest_Security_Level() {
        AuthenticationMethod[] methods = [new SmsAuthenticationMethod(new IdentityMessageDescriber()), new EmailAuthenticationMethod(new IdentityMessageDescriber())];

        Assert.Equal(AuthenticationMethodType.Email, methods.SelectPreferred(nameof(AuthenticationMethodType.Email))?.Type);
        Assert.Equal(AuthenticationMethodType.Email, methods.SelectPreferred("email")?.Type);
        Assert.Equal(AuthenticationMethodType.PhoneNumber, methods.SelectPreferred(nameof(AuthenticationMethodType.PhoneNumber))?.Type);
        Assert.Equal(AuthenticationMethodType.PhoneNumber, methods.SelectPreferred(null)?.Type);
        Assert.Equal(AuthenticationMethodType.PhoneNumber, methods.SelectPreferred("Unknown")?.Type);
    }

    [Fact]
    public void SelectPreferred_Never_Selects_A_Less_Secure_Method() {
        AuthenticationMethod[] methods = [
            new SmsAuthenticationMethod(new IdentityMessageDescriber()),
            new EmailAuthenticationMethod(new IdentityMessageDescriber()),
            new AuthenticatorAppAuthenticationMethod(new IdentityMessageDescriber())
        ];

        Assert.Equal(AuthenticationMethodType.AuthenticatorApp, methods.SelectPreferred(nameof(AuthenticationMethodType.Email))?.Type);
        Assert.Null(Array.Empty<AuthenticationMethod>().SelectPreferred(nameof(AuthenticationMethodType.Email)));
    }

    [Fact]
    public async Task FindMethodForUserOrDefault_Uses_Two_Factor_Preference() {
        var user = await CreateUserAsync(twoFactorEnabled: false);
        var provider = CreateProvider();

        Assert.Equal(AuthenticationMethodType.PhoneNumber, (await provider.FindMethodForUserOrDefaultAsync(user))?.Type);
        Assert.True((await UserManager.SetTwoFactorAsync(user, nameof(AuthenticationMethodType.Email), TestContext.Current.CancellationToken)).Succeeded);
        Assert.Equal(AuthenticationMethodType.Email, (await provider.FindMethodForUserOrDefaultAsync(user))?.Type);
    }

    [Fact]
    public async Task SetTwoFactor_Persists_Preference_When_Two_Factor_Is_Already_Enabled() {
        var user = await CreateUserAsync(twoFactorEnabled: true);

        var result = await UserManager.SetTwoFactorAsync(user, nameof(AuthenticationMethodType.Email), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        await using var scope = _rootProvider.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var storedUser = await userManager.FindByIdAsync(user.Id);
        Assert.NotNull(storedUser);
        Assert.True(await userManager.GetTwoFactorEnabledAsync(storedUser));
        Assert.Equal(nameof(AuthenticationMethodType.Email), await userManager.GetTwoFactorPreferenceAsync(storedUser, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReplaceAuthenticatorKey_Stores_Key_Only_With_Valid_Code() {
        var user = await CreateUserAsync(twoFactorEnabled: false);
        var key = UserManager.GenerateNewAuthenticatorKey();

        var invalid = await UserManager.ReplaceAuthenticatorKeyAsync(user, key, GenerateCode(key) == "000000" ? "111111" : "000000", TestContext.Current.CancellationToken);
        Assert.False(invalid.Succeeded);
        Assert.Null(await UserManager.GetAuthenticatorKeyAsync(user));

        var nonNumeric = await UserManager.ReplaceAuthenticatorKeyAsync(user, key, "abcdef", TestContext.Current.CancellationToken);
        Assert.False(nonNumeric.Succeeded);

        var valid = await UserManager.ReplaceAuthenticatorKeyAsync(user, key, GenerateCode(key), TestContext.Current.CancellationToken);
        Assert.True(valid.Succeeded);
        Assert.Equal(key, await UserManager.GetAuthenticatorKeyAsync(user));
        // The stored key works with the authenticator token provider used at sign in.
        Assert.True(await UserManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, GenerateCode(key)));
    }

    [Fact]
    public async Task ReplaceAuthenticatorKey_Keeps_Current_Key_When_Code_Is_Invalid() {
        var user = await CreateUserAsync(twoFactorEnabled: true);
        await UserManager.ResetAuthenticatorKeyAsync(user);
        var currentKey = await UserManager.GetAuthenticatorKeyAsync(user);
        var newKey = UserManager.GenerateNewAuthenticatorKey();

        var result = await UserManager.ReplaceAuthenticatorKeyAsync(user, newKey, GenerateCode(currentKey!), TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(currentKey, await UserManager.GetAuthenticatorKeyAsync(user));
    }

    internal static string GenerateCode(string base32Key) =>
        new Rfc6238AuthenticationService(timestep: 0.5, codeLength: 6).GenerateCode(FromBase32(base32Key)).ToString("D6");

    private static byte[] FromBase32(string input) {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bits = 0;
        var value = 0;
        var output = new List<byte>();
        foreach (var character in input.TrimEnd('=').ToUpperInvariant()) {
            value = (value << 5) | alphabet.IndexOf(character);
            bits += 5;
            if (bits >= 8) {
                output.Add((byte)(value >> (bits - 8)));
                bits -= 8;
            }
        }
        return [.. output];
    }

    private AuthenticationMethodProviderInMemory CreateProvider() => new(
        Enumerable.Empty<IHubContext<MultiFactorAuthenticationHub>>(),
        _scope.ServiceProvider.GetRequiredService<IConfiguration>(),
        UserManager,
        [
            new SmsAuthenticationMethod(new IdentityMessageDescriber()),
            new EmailAuthenticationMethod(new IdentityMessageDescriber())
        ],
        [
            new AuthenticationMethodConfiguration { MethodType = typeof(SmsAuthenticationMethod) },
            new AuthenticationMethodConfiguration { MethodType = typeof(EmailAuthenticationMethod) }
        ]
    );

    private async Task<User> CreateUserAsync(bool twoFactorEnabled) {
        var user = new User {
            UserName = "someone",
            Email = "someone@somewhere.com",
            EmailConfirmed = true,
            PhoneNumber = "+306900000000",
            PhoneNumberConfirmed = true,
            TwoFactorEnabled = twoFactorEnabled
        };
        Assert.True((await UserManager.CreateAsync(user)).Succeeded);
        return user;
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() {
        await _scope.DisposeAsync();
        await _rootProvider.DisposeAsync();
    }
}
