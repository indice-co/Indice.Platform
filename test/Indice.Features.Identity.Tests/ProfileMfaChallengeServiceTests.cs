#if NET9_0_OR_GREATER
using Duende.IdentityServer.Services;
#else
using IdentityServer4.Services;
#endif
using Indice.Events;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Data.Stores;
using Indice.Features.Identity.Core.Events;
using Indice.Features.Identity.Core.Models;
using Indice.Features.Identity.Core.Totp;
using Indice.Features.Identity.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Indice.Features.Identity.Tests;

public class ProfileMfaChallengeServiceTests : IAsyncLifetime
{
    private const string SessionId = "session-1";
    private readonly string _databaseName = Guid.NewGuid().ToString();
    private readonly ServiceProvider _rootProvider;
    private readonly List<AsyncServiceScope> _scopes = [];
    private readonly TestHttpContextAccessor _httpContextAccessor = new();
    private readonly FakeTimeProvider _timeProvider = new(DateTimeOffset.UtcNow);
    private readonly Mock<IAuthenticationMethodProvider> _methodProvider = new();
    private readonly Mock<IUserSession> _userSession = new();
    private string? _sessionId = SessionId;
    private AuthenticationMethod[] _methods = [];
    private bool _allowDowngrade;

    public ProfileMfaChallengeServiceTests() {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var services = new ServiceCollection()
            .AddSingleton(configuration)
            .AddLogging()
            .AddSingleton<IHttpContextAccessor>(_httpContextAccessor);
        services.AddTransient<IUserRequirementProvider<User>, UserRequirementProviderNoOp>();
        services.AddTotpServiceFactory(configuration)
                .AddDefaultPlatformEventService()
                .AddSmsServiceNoop()
                .AddPushNotificationServiceNoop()
                .AddLocalization()
                .AddDistributedMemoryCache()
                .AddDbContext<ExtendedIdentityDbContext<User, Role>>(builder => builder.UseInMemoryDatabase(_databaseName))
                .AddIdentity<User, Role>()
                .AddDefaultTokenProviders()
                .AddExtendedUserManager()
                .AddExtendedSignInManager()
                .AddEntityFrameworkStores<ExtendedIdentityDbContext<User, Role>>()
                .AddUserStore<ExtendedUserStore<ExtendedIdentityDbContext<User, Role>, User, Role>>()
                .AddExtendedPhoneNumberTokenProvider(configuration)
                .AddIdentityMessageDescriber<IdentityMessageDescriber>();
        // The account locked handler signs the user out through IdentityServer, which is not part of this test.
        services.RemoveAll<IPlatformEventHandler<AccountLockedEvent>>();
        services.AddAuthentication().AddCookie(ExtendedIdentityConstants.ProfileMfaChallengeScheme, options => {
            options.Cookie.Name = ExtendedIdentityConstants.ProfileMfaChallengeCookieName;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            options.SlidingExpiration = false;
        });
        _rootProvider = services.BuildServiceProvider();
        _userSession.Setup(x => x.GetSessionIdAsync()).ReturnsAsync(() => _sessionId);
        _methodProvider.Setup(x => x.GetAllMethodsForUserAsync(It.IsAny<User>())).ReturnsAsync(() => _methods);
        _methodProvider.SetupGet(x => x.AllowMfaChannelDowngrade).Returns(() => _allowDowngrade);
        NewRequest();
    }

    private IServiceProvider Services => _scopes[^1].ServiceProvider;
    private ExtendedUserManager<User> UserManager => Services.GetRequiredService<ExtendedUserManager<User>>();

    private static AuthenticationMethod Sms => new SmsAuthenticationMethod(new IdentityMessageDescriber());
    private static AuthenticationMethod Email => new EmailAuthenticationMethod(new IdentityMessageDescriber());
    private static AuthenticationMethod AuthenticatorApp => new AuthenticatorAppAuthenticationMethod(new IdentityMessageDescriber());
    private static AuthenticationMethod TrustedDevice => new TrustedDeviceAuthenticationMethod(new IdentityMessageDescriber());

    private ProfileMfaChallengeService CreateService() => new(
        _httpContextAccessor,
        UserManager,
        Services.GetRequiredService<ExtendedSignInManager<User>>(),
        _methodProvider.Object,
        Services.GetRequiredService<TotpServiceFactory>(),
        _userSession.Object,
        _timeProvider,
        Options.Create(new IdentityUIOptions()),
        NullLogger<ProfileMfaChallengeService>.Instance
    );

    /// <summary>Starts a new request (with its own service scope, like ASP.NET Core) that carries the cookies issued by the previous one.</summary>
    private void NewRequest() {
        var previous = _httpContextAccessor.HttpContext;
        _scopes.Add(_rootProvider.CreateAsyncScope());
        var context = new DefaultHttpContext { RequestServices = Services };
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");
        if (previous is not null) {
            var cookies = previous.Response.Headers.SetCookie
                .Select(header => header!.Split(';')[0])
                .Where(cookie => !cookie.EndsWith('='));
            context.Request.Headers.Cookie = string.Join("; ", cookies);
        }
        _httpContextAccessor.HttpContext = context;
    }

    private async Task<User> CreateUserAsync(bool twoFactorEnabled = true, string email = "someone@somewhere.com") {
        var user = new User {
            Email = email,
            UserName = email,
            EmailConfirmed = true,
            PhoneNumber = "+306900000000",
            PhoneNumberConfirmed = true,
            TwoFactorEnabled = twoFactorEnabled,
            LockoutEnabled = true
        };
        var result = await UserManager.CreateAsync(user);
        Assert.True(result.Succeeded);
        return user;
    }

    private async Task<User> NewRequestAsync(User user) {
        NewRequest();
        return (await UserManager.FindByIdAsync(user.Id))!;
    }

    private Task<string> GenerateCodeAsync(User user, AuthenticationMethod method) =>
        UserManager.GenerateUserTokenAsync(user, method.GetTokenProvider()!, TotpConstants.TokenGenerationPurpose.ProfileMfaChallenge);

    private static string WrongCode(string code) => code == "000000" ? "111111" : "000000";

    [Fact]
    public async Task GetStatus_Is_NotRequired_When_Mfa_Is_Disabled() {
        _methods = [Sms];
        var user = await CreateUserAsync(twoFactorEnabled: false);

        Assert.Equal(ProfileMfaChallengeStatus.NotRequired, await CreateService().GetStatusAsync(user));
    }

    [Fact]
    public async Task GetStatus_Is_Required_When_Mfa_Is_Enabled() {
        _methods = [Sms, Email];
        var user = await CreateUserAsync();

        Assert.Equal(ProfileMfaChallengeStatus.Required, await CreateService().GetStatusAsync(user));
    }

    [Fact]
    public async Task GetStatus_Is_Blocked_When_No_Code_Based_Method_Is_Available() {
        _methods = [TrustedDevice];
        var user = await CreateUserAsync();

        Assert.Equal(ProfileMfaChallengeStatus.Blocked, await CreateService().GetStatusAsync(user));
    }

    [Fact]
    public async Task GetStatus_Is_Blocked_When_There_Is_No_Session_Id() {
        _methods = [Sms];
        _sessionId = null;
        var user = await CreateUserAsync();

        Assert.Equal(ProfileMfaChallengeStatus.Blocked, await CreateService().GetStatusAsync(user));
    }

    [Fact]
    public async Task GetMethods_Excludes_Push_And_Keeps_Code_Based_Methods_In_Order() {
        _methods = [TrustedDevice, AuthenticatorApp, Sms, Email];
        var user = await CreateUserAsync();

        var methods = await CreateService().GetMethodsAsync(user);

        Assert.Equal(["AuthenticatorApp", "Sms", "Email"], methods.Select(method => method.Code));
    }

    [Fact]
    public async Task FindMethod_Ignores_Requested_Method_When_Downgrade_Is_Not_Allowed() {
        _methods = [TrustedDevice, Sms, Email];
        _allowDowngrade = false;
        var user = await CreateUserAsync();

        var method = await CreateService().FindMethodAsync(user, "Email");

        Assert.Equal("Sms", method?.Code);
    }

    [Theory]
    [InlineData("Email", "Email")]
    [InlineData("email", "Email")]
    [InlineData("TrustedDevice", "Sms")]
    [InlineData("Unknown", "Sms")]
    [InlineData(null, "Sms")]
    public async Task FindMethod_Honours_Requested_Method_When_Downgrade_Is_Allowed(string? requested, string expected) {
        _methods = [TrustedDevice, Sms, Email];
        _allowDowngrade = true;
        var user = await CreateUserAsync();

        var method = await CreateService().FindMethodAsync(user, requested);

        Assert.Equal(expected, method?.Code);
    }

    [Fact]
    public async Task VerifyCode_Succeeds_And_Satisfies_The_Mfa_Challenge_For_The_Same_Session() {
        _methods = [Email];
        var user = await CreateUserAsync();
        var code = await GenerateCodeAsync(user, Email);

        var result = await CreateService().VerifyCodeAsync(user, Email, code);
        user = await NewRequestAsync(user);

        Assert.Equal(ProfileMfaChallengeVerifyResult.Succeeded, result);
        Assert.Equal(ProfileMfaChallengeStatus.Satisfied, await CreateService().GetStatusAsync(user));
    }

    [Fact]
    public async Task VerifyCode_Does_Not_Change_The_Security_Stamp() {
        _methods = [Email];
        var user = await CreateUserAsync();
        var securityStamp = await UserManager.GetSecurityStampAsync(user);
        var code = await GenerateCodeAsync(user, Email);

        await CreateService().VerifyCodeAsync(user, Email, code);

        Assert.Equal(securityStamp, await UserManager.GetSecurityStampAsync(user));
    }

    [Fact]
    public async Task VerifyCode_Rejects_Code_Generated_For_Another_Purpose() {
        _methods = [Email];
        var user = await CreateUserAsync();
        var mfaCode = await UserManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);

        var result = await CreateService().VerifyCodeAsync(user, Email, mfaCode);

        Assert.Equal(ProfileMfaChallengeVerifyResult.Failed, result);
    }

    [Fact]
    public async Task VerifyCode_Fails_And_Counts_Towards_Lockout_When_Code_Is_Invalid() {
        _methods = [Email];
        var user = await CreateUserAsync();
        var code = await GenerateCodeAsync(user, Email);

        var result = await CreateService().VerifyCodeAsync(user, Email, WrongCode(code));
        user = await NewRequestAsync(user);

        Assert.Equal(ProfileMfaChallengeVerifyResult.Failed, result);
        Assert.Equal(1, await UserManager.GetAccessFailedCountAsync(user));
        Assert.Equal(ProfileMfaChallengeStatus.Required, await CreateService().GetStatusAsync(user));
    }

    [Fact]
    public async Task VerifyCode_Locks_Out_User_After_Too_Many_Invalid_Codes() {
        _methods = [Email];
        var user = await CreateUserAsync();
        var code = await GenerateCodeAsync(user, Email);
        var maxAttempts = UserManager.Options.Lockout.MaxFailedAccessAttempts;
        var service = CreateService();

        var results = new List<ProfileMfaChallengeVerifyResult>();
        for (var attempt = 0; attempt < maxAttempts; attempt++) {
            results.Add(await service.VerifyCodeAsync(user, Email, WrongCode(code)));
        }

        Assert.All(results.SkipLast(1), result => Assert.Equal(ProfileMfaChallengeVerifyResult.Failed, result));
        Assert.Equal(ProfileMfaChallengeVerifyResult.LockedOut, results[^1]);
        Assert.True(await UserManager.IsLockedOutAsync(user));
        Assert.Equal(ProfileMfaChallengeVerifyResult.LockedOut, await service.VerifyCodeAsync(user, Email, code));
    }

    [Fact]
    public async Task VerifyCode_Fails_When_Method_Is_Not_Available_To_User() {
        _methods = [Sms];
        var user = await CreateUserAsync();
        var code = await GenerateCodeAsync(user, Email);

        var result = await CreateService().VerifyCodeAsync(user, Email, code);

        Assert.Equal(ProfileMfaChallengeVerifyResult.Failed, result);
    }

    [Fact]
    public async Task Mfa_Challenge_Is_Not_Satisfied_For_Another_Session() {
        _methods = [Email];
        var user = await CreateUserAsync();
        Assert.Equal(ProfileMfaChallengeVerifyResult.Succeeded, await CreateService().VerifyCodeAsync(user, Email, await GenerateCodeAsync(user, Email)));
        user = await NewRequestAsync(user);
        _sessionId = "session-2";

        Assert.Equal(ProfileMfaChallengeStatus.Required, await CreateService().GetStatusAsync(user));
    }

    [Fact]
    public async Task Mfa_Challenge_Is_Not_Satisfied_For_Another_User() {
        _methods = [Email];
        var user = await CreateUserAsync();
        var otherUser = await CreateUserAsync(email: "other@somewhere.com");
        Assert.Equal(ProfileMfaChallengeVerifyResult.Succeeded, await CreateService().VerifyCodeAsync(user, Email, await GenerateCodeAsync(user, Email)));
        otherUser = await NewRequestAsync(otherUser);

        Assert.Equal(ProfileMfaChallengeStatus.Required, await CreateService().GetStatusAsync(otherUser));
    }

    [Fact]
    public async Task Mfa_Challenge_Expires_After_Lifetime() {
        _methods = [Email];
        var user = await CreateUserAsync();
        Assert.Equal(ProfileMfaChallengeVerifyResult.Succeeded, await CreateService().VerifyCodeAsync(user, Email, await GenerateCodeAsync(user, Email)));
        user = await NewRequestAsync(user);
        _timeProvider.Advance(TimeSpan.FromMinutes(4));
        Assert.Equal(ProfileMfaChallengeStatus.Satisfied, await CreateService().GetStatusAsync(user));

        _timeProvider.Advance(TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(1));

        Assert.Equal(ProfileMfaChallengeStatus.Required, await CreateService().GetStatusAsync(user));
    }

    [Fact]
    public async Task Clear_Removes_The_Mfa_Challenge() {
        _methods = [Email];
        var user = await CreateUserAsync();
        Assert.Equal(ProfileMfaChallengeVerifyResult.Succeeded, await CreateService().VerifyCodeAsync(user, Email, await GenerateCodeAsync(user, Email)));
        user = await NewRequestAsync(user);
        Assert.Equal(ProfileMfaChallengeStatus.Satisfied, await CreateService().GetStatusAsync(user));

        await CreateService().ClearAsync();
        user = await NewRequestAsync(user);

        Assert.Equal(ProfileMfaChallengeStatus.Required, await CreateService().GetStatusAsync(user));
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() {
        foreach (var scope in _scopes) {
            await scope.DisposeAsync();
        }
        await _rootProvider.DisposeAsync();
    }

    private sealed class TestHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan delta) => _now = _now.Add(delta);
    }
}
