using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Events;
using Indice.Features.Identity.Core.Data.Stores;
using Indice.Features.Identity.Core.Events;
using Indice.Features.Identity.Core.Hubs;
using Indice.Features.Identity.Core.Models;
using Indice.Features.Identity.UI;
using Indice.Features.Identity.UI.Models;
using Indice.Features.Identity.UI.Pages;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Moq;

namespace Indice.Features.Identity.Tests;

public class ManageMfaPageTests : IAsyncLifetime
{
    private readonly Mock<IProfileMfaChallengeService> _mfaChallengeService = new();
    private readonly Mock<ITempDataProvider> _tempDataProvider = new();
    private readonly IdentityUIOptions _uiOptions = new();
    private readonly IDataProtectionProvider _dataProtectionProvider = new EphemeralDataProtectionProvider();
    private ProfileMfaChallengeStatus _status = ProfileMfaChallengeStatus.NotRequired;
    private ServiceProvider? _rootProvider;
    private AsyncServiceScope _scope;
    private HttpContext _httpContext = null!;
    private IConfiguration _configuration = null!;

    public ManageMfaPageTests() {
        var sms = new SmsAuthenticationMethod(new IdentityMessageDescriber());
        _mfaChallengeService.Setup(x => x.GetStatusAsync(It.IsAny<User>())).ReturnsAsync(() => _status);
        _mfaChallengeService.Setup(x => x.GetMethodsAsync(It.IsAny<User>())).ReturnsAsync([sms]);
        _mfaChallengeService.Setup(x => x.FindMethodAsync(It.IsAny<User>(), It.IsAny<string?>())).ReturnsAsync(sms);
        _tempDataProvider.Setup(x => x.LoadTempData(It.IsAny<HttpContext>())).Returns(new Dictionary<string, object>());
    }

    private ExtendedUserManager<User> UserManager => _scope.ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();

    private async Task<User> SetupAsync(bool twoFactorEnabled = false, bool phoneConfirmed = true) {
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["General:ApplicationName"] = "Test"
        }).Build();
        var httpContextAccessor = new TestHttpContextAccessor();
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection()
            .AddSingleton(_configuration)
            .AddLogging()
            .AddSingleton<IHttpContextAccessor>(httpContextAccessor)
            .AddSingleton(_mfaChallengeService.Object);
        services.AddTransient<IUserRequirementProvider<User>, UserRequirementProviderNoOp>();
        services.AddTotpServiceFactory(_configuration)
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
        _httpContext = new DefaultHttpContext { RequestServices = _scope.ServiceProvider };
        _httpContext.Request.Scheme = "https";
        _httpContext.Request.Host = new HostString("localhost");
        httpContextAccessor.HttpContext = _httpContext;
        var user = new User {
            UserName = "someone",
            Email = "someone@somewhere.com",
            EmailConfirmed = true,
            PhoneNumber = "+306900000000",
            PhoneNumberConfirmed = phoneConfirmed,
            TwoFactorEnabled = twoFactorEnabled
        };
        Assert.True((await UserManager.CreateAsync(user)).Succeeded);
        var signInManager = _scope.ServiceProvider.GetRequiredService<ExtendedSignInManager<User>>();
        _httpContext.User = await signInManager.CreateUserPrincipalAsync(user);
        return user;
    }

    private TestManageMfaModel CreatePage(ITempDataDictionary? tempData = null) {
        var actionContext = new ActionContext(_httpContext, new RouteData(), new CompiledPageActionDescriptor(), new ModelStateDictionary());
        var provider = new AuthenticationMethodProviderInMemory(
            Enumerable.Empty<IHubContext<MultiFactorAuthenticationHub>>(),
            _configuration,
            UserManager,
            [
                new SmsAuthenticationMethod(new IdentityMessageDescriber()),
                new EmailAuthenticationMethod(new IdentityMessageDescriber()),
                new AuthenticatorAppAuthenticationMethod(new IdentityMessageDescriber()),
                new RecoveryCodeAuthenticationMethod(new IdentityMessageDescriber())
            ],
            [
                new AuthenticationMethodConfiguration { MethodType = typeof(SmsAuthenticationMethod) },
                new AuthenticationMethodConfiguration { MethodType = typeof(EmailAuthenticationMethod) },
                new AuthenticationMethodConfiguration { MethodType = typeof(AuthenticatorAppAuthenticationMethod) },
                new AuthenticationMethodConfiguration { MethodType = typeof(RecoveryCodeAuthenticationMethod) }
            ]
        );
        return new TestManageMfaModel(
            UserManager,
            _scope.ServiceProvider.GetRequiredService<ExtendedSignInManager<User>>(),
            provider,
            _dataProtectionProvider,
            _configuration,
            Options.Create(_uiOptions),
            new IdentityUILocalizer()
        ) {
            PageContext = new PageContext(actionContext),
            TempData = tempData ?? new TempDataDictionary(_httpContext, _tempDataProvider.Object)
        };
    }

    private async Task<User> ReloadUserAsync(User user) {
        await using var scope = _rootProvider!.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExtendedIdentityDbContext<User, Role>>();
        return await dbContext.Users.AsNoTracking().SingleAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task OnGet_Lists_Methods_And_Status() {
        await SetupAsync(phoneConfirmed: false);
        var page = CreatePage();

        var result = await page.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Null(page.MfaChallenge);
        Assert.False(page.View.TwoFactorEnabled);
        Assert.Null(page.View.CurrentMethod);
        Assert.False(page.View.CanRegenerateRecoveryCodes);
        Assert.Equal([AuthenticationMethodType.PhoneNumber, AuthenticationMethodType.Email, AuthenticationMethodType.AuthenticatorApp], page.View.Methods.Select(x => x.Type));
        Assert.False(page.View.Methods[0].IsAvailable);
        Assert.True(page.View.Methods[1].IsAvailable);
    }

    [Fact]
    public async Task OnGet_Shows_Mfa_Challenge_When_Required() {
        await SetupAsync(twoFactorEnabled: true);
        _status = ProfileMfaChallengeStatus.Required;
        var page = CreatePage();

        var result = await page.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.NotNull(page.MfaChallenge);
    }

    [Fact]
    public async Task OnPostSelectMethod_Turns_On_Two_Factor_With_Selected_Method() {
        var user = await SetupAsync();
        var page = CreatePage();

        var result = await page.OnPostSelectMethodAsync(AuthenticationMethodType.Email);

        Assert.Equal("/Profile", Assert.IsType<RedirectToPageResult>(result).PageName);
        Assert.True(page.TempData.ContainsKey("Alert"));
        var stored = await ReloadUserAsync(user);
        Assert.True(stored.TwoFactorEnabled);
        Assert.Equal(nameof(AuthenticationMethodType.Email), await UserManager.GetTwoFactorPreferenceAsync(stored, TestContext.Current.CancellationToken));
        var reloaded = CreatePage();
        await reloaded.OnGetAsync();
        Assert.Equal(AuthenticationMethodType.Email, reloaded.View.CurrentMethod?.Type);
    }

    [Theory]
    [InlineData(ProfileMfaChallengeStatus.Required)]
    [InlineData(ProfileMfaChallengeStatus.Blocked)]
    public async Task OnPostSelectMethod_Does_Not_Change_Method_Until_Mfa_Challenge_Is_Satisfied(ProfileMfaChallengeStatus status) {
        var user = await SetupAsync(twoFactorEnabled: true);
        _status = status;
        var page = CreatePage();

        var result = await page.OnPostSelectMethodAsync(AuthenticationMethodType.Email);

        Assert.IsType<PageResult>(result);
        Assert.NotNull(page.MfaChallenge);
        Assert.Null(await UserManager.GetTwoFactorPreferenceAsync(await ReloadUserAsync(user), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task OnPostSelectMethod_Rejects_Unavailable_Method() {
        var user = await SetupAsync(phoneConfirmed: false);
        var page = CreatePage();

        var result = await page.OnPostSelectMethodAsync(AuthenticationMethodType.PhoneNumber);

        Assert.IsType<PageResult>(result);
        Assert.False(page.ModelState.IsValid);
        Assert.False((await ReloadUserAsync(user)).TwoFactorEnabled);
    }

    [Fact]
    public async Task OnPostSelectMethod_Removes_Authenticator_Key_When_Switching_Away_From_Authenticator() {
        var user = await SetupAsync();
        await UserManager.ResetAuthenticatorKeyAsync(user);
        Assert.True((await UserManager.SetTwoFactorAsync(user, nameof(AuthenticationMethodType.AuthenticatorApp), TestContext.Current.CancellationToken)).Succeeded);
        var page = CreatePage();
        await page.OnGetAsync();
        Assert.Equal(AuthenticationMethodType.AuthenticatorApp, page.View.CurrentMethod?.Type);
        Assert.True(page.View.CanRegenerateRecoveryCodes);

        var result = await CreatePage().OnPostSelectMethodAsync(AuthenticationMethodType.PhoneNumber);

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Null(await UserManager.GetAuthenticatorKeyAsync(user));
        var reloaded = CreatePage();
        await reloaded.OnGetAsync();
        Assert.Equal(AuthenticationMethodType.PhoneNumber, reloaded.View.CurrentMethod?.Type);
        Assert.False(reloaded.View.CanRegenerateRecoveryCodes);
    }

    [Fact]
    public async Task Authenticator_Setup_Stores_Key_Only_After_Valid_Code() {
        var user = await SetupAsync();
        var setupPage = CreatePage();

        var setupResult = await setupPage.OnPostSetupAuthenticatorAsync();

        Assert.IsType<PageResult>(setupResult);
        Assert.NotNull(setupPage.Setup);
        Assert.Contains("otpauth://totp/Test:", setupPage.Setup.AuthenticatorUri);
        Assert.Null(await UserManager.GetAuthenticatorKeyAsync(user));
        var key = setupPage.Setup.SharedKey!;
        var validCode = TwoFactorPreferenceTests.GenerateCode(key);

        var invalidPage = CreatePage();
        var invalidResult = await invalidPage.OnPostVerifyAuthenticatorAsync(setupPage.Setup.ProtectedKey, validCode == "000000" ? "111111" : "000000");
        Assert.IsType<PageResult>(invalidResult);
        Assert.True(invalidPage.ModelState.ContainsKey("code"));
        Assert.NotNull(invalidPage.Setup);
        Assert.Null(await UserManager.GetAuthenticatorKeyAsync(user));

        var verifyPage = CreatePage();
        var verifyResult = await verifyPage.OnPostVerifyAuthenticatorAsync(setupPage.Setup.ProtectedKey, validCode);

        var redirect = Assert.IsType<RedirectToPageResult>(verifyResult);
        Assert.Equal("RecoveryCodes", redirect.PageHandler);
        Assert.Equal(key, await UserManager.GetAuthenticatorKeyAsync(user));
        Assert.True((await ReloadUserAsync(user)).TwoFactorEnabled);
        var codesPage = CreatePage(verifyPage.TempData);
        Assert.IsType<PageResult>(await codesPage.OnGetRecoveryCodesAsync());
        Assert.Equal(10, codesPage.RecoveryCodes?.RecoveryCodes.Length);
        Assert.Equal(10, await UserManager.CountRecoveryCodesAsync(user));
    }

    [Fact]
    public async Task OnPostVerifyAuthenticator_Rejects_Tampered_Key() {
        var user = await SetupAsync();
        var page = CreatePage();

        var result = await page.OnPostVerifyAuthenticatorAsync("tampered", "123456");

        Assert.IsType<PageResult>(result);
        Assert.False(page.ModelState.IsValid);
        Assert.Null(page.Setup);
        Assert.Null(await UserManager.GetAuthenticatorKeyAsync(user));
    }

    [Fact]
    public async Task OnPostVerifyAuthenticator_Rejects_Key_Created_For_Another_User() {
        var user = await SetupAsync();
        var key = UserManager.GenerateNewAuthenticatorKey();
        var protectedKey = _dataProtectionProvider.CreateProtector("Indice.Features.Identity.UI.ManageMfa.PendingAuthenticatorKey").ToTimeLimitedDataProtector().Protect($"another-user|{key}", TimeSpan.FromMinutes(10));

        var result = await CreatePage().OnPostVerifyAuthenticatorAsync(protectedKey, TwoFactorPreferenceTests.GenerateCode(key));

        Assert.IsType<BadRequestResult>(result);
        Assert.Null(await UserManager.GetAuthenticatorKeyAsync(user));
    }

    [Fact]
    public async Task OnPostRegenerateRecoveryCodes_Requires_Authenticator() {
        await SetupAsync(twoFactorEnabled: true);

        var result = await CreatePage().OnPostRegenerateRecoveryCodesAsync();

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task OnPostRegenerateRecoveryCodes_Replaces_Recovery_Codes() {
        var user = await SetupAsync();
        await UserManager.ResetAuthenticatorKeyAsync(user);
        Assert.True((await UserManager.SetTwoFactorAsync(user, nameof(AuthenticationMethodType.AuthenticatorApp), TestContext.Current.CancellationToken)).Succeeded);
        var oldCodes = (await UserManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10))!.ToArray();
        var page = CreatePage();

        var result = await page.OnPostRegenerateRecoveryCodesAsync();

        Assert.Equal("RecoveryCodes", Assert.IsType<RedirectToPageResult>(result).PageHandler);
        Assert.False((await UserManager.RedeemTwoFactorRecoveryCodeAsync(user, oldCodes[0])).Succeeded);
        var codesPage = CreatePage(page.TempData);
        await codesPage.OnGetRecoveryCodesAsync();
        Assert.True((await UserManager.RedeemTwoFactorRecoveryCodeAsync(user, codesPage.RecoveryCodes!.RecoveryCodes[0])).Succeeded);
    }

    [Fact]
    public async Task OnPostRegenerateRecoveryCodes_Requires_Mfa_Challenge() {
        var user = await SetupAsync();
        await UserManager.ResetAuthenticatorKeyAsync(user);
        Assert.True((await UserManager.SetTwoFactorAsync(user, nameof(AuthenticationMethodType.AuthenticatorApp), TestContext.Current.CancellationToken)).Succeeded);
        _status = ProfileMfaChallengeStatus.Required;
        var page = CreatePage();

        var result = await page.OnPostRegenerateRecoveryCodesAsync();

        Assert.IsType<PageResult>(result);
        Assert.NotNull(page.MfaChallenge);
        Assert.Equal(0, await UserManager.CountRecoveryCodesAsync(user));
    }

    [Fact]
    public async Task Page_Is_Disabled_By_Option() {
        var user = await SetupAsync();
        _uiOptions.DisableMfaEdit = true;

        var get = await CreatePage().OnGetAsync();
        var select = await CreatePage().OnPostSelectMethodAsync(AuthenticationMethodType.Email);
        var setup = await CreatePage().OnPostSetupAuthenticatorAsync();
        var regenerate = await CreatePage().OnPostRegenerateRecoveryCodesAsync();

        Assert.Equal("/Profile", Assert.IsType<RedirectToPageResult>(get).PageName);
        Assert.IsType<BadRequestResult>(select);
        Assert.IsType<BadRequestResult>(setup);
        Assert.IsType<BadRequestResult>(regenerate);
        Assert.False((await ReloadUserAsync(user)).TwoFactorEnabled);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() {
        await _scope.DisposeAsync();
        if (_rootProvider is not null) {
            await _rootProvider.DisposeAsync();
        }
    }

    private sealed class TestManageMfaModel(
        ExtendedUserManager<User> userManager,
        ExtendedSignInManager<User> signInManager,
        IAuthenticationMethodProvider authenticationMethodProvider,
        IDataProtectionProvider dataProtectionProvider,
        IConfiguration configuration,
        IOptions<IdentityUIOptions> identityUiOptions,
        IdentityUILocalizer localizer
    ) : BaseManageMfaModel(userManager, signInManager, authenticationMethodProvider, dataProtectionProvider, configuration, identityUiOptions, localizer);

    private sealed class TestHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }
}
