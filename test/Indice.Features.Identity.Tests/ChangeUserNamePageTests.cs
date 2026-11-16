using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Data.Stores;
using Indice.Features.Identity.Core.Models;
using Indice.Features.Identity.UI;
using Indice.Features.Identity.UI.Models;
using Indice.Features.Identity.UI.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Indice.Features.Identity.Tests;

public class ChangeUserNamePageTests : IAsyncLifetime
{
    private const string CurrentUserName = "someone";
    private readonly Mock<IProfileMfaChallengeService> _mfaChallengeService = new();
    private readonly Mock<ITempDataProvider> _tempDataProvider = new();
    private readonly IdentityUIOptions _uiOptions = new();
    private ProfileMfaChallengeStatus _status = ProfileMfaChallengeStatus.NotRequired;
    private ServiceProvider? _rootProvider;
    private AsyncServiceScope _scope;
    private HttpContext _httpContext = null!;

    public ChangeUserNamePageTests() {
        var sms = new SmsAuthenticationMethod(new IdentityMessageDescriber());
        _mfaChallengeService.Setup(x => x.GetStatusAsync(It.IsAny<User>())).ReturnsAsync(() => _status);
        _mfaChallengeService.Setup(x => x.GetMethodsAsync(It.IsAny<User>())).ReturnsAsync([sms]);
        _mfaChallengeService.Setup(x => x.FindMethodAsync(It.IsAny<User>(), It.IsAny<string?>())).ReturnsAsync(sms);
        _tempDataProvider.Setup(x => x.LoadTempData(It.IsAny<HttpContext>())).Returns(new Dictionary<string, object>());
    }

    private ExtendedUserManager<User> UserManager => _scope.ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();

    private async Task<User> SetupAsync(bool emailAsUserName = false) {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["IdentityOptions:User:EmailAsUserName"] = emailAsUserName.ToString()
        }).Build();
        var httpContextAccessor = new TestHttpContextAccessor();
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection()
            .AddSingleton(configuration)
            .AddLogging()
            .AddSingleton<IHttpContextAccessor>(httpContextAccessor)
            .AddSingleton(_mfaChallengeService.Object);
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
        _rootProvider = services.BuildServiceProvider();
        _scope = _rootProvider.CreateAsyncScope();
        _httpContext = new DefaultHttpContext { RequestServices = _scope.ServiceProvider };
        _httpContext.Request.Scheme = "https";
        _httpContext.Request.Host = new HostString("localhost");
        httpContextAccessor.HttpContext = _httpContext;
        var user = new User {
            UserName = CurrentUserName,
            Email = "someone@somewhere.com",
            EmailConfirmed = true,
            TwoFactorEnabled = true
        };
        Assert.True((await UserManager.CreateAsync(user)).Succeeded);
        var signInManager = _scope.ServiceProvider.GetRequiredService<ExtendedSignInManager<User>>();
        _httpContext.User = await signInManager.CreateUserPrincipalAsync(user);
        return user;
    }

    private TestChangeUserNameModel CreatePage(string? userName = null) {
        var actionContext = new ActionContext(_httpContext, new RouteData(), new CompiledPageActionDescriptor(), new ModelStateDictionary());
        return new TestChangeUserNameModel(UserManager, Options.Create(_uiOptions)) {
            PageContext = new PageContext(actionContext),
            TempData = new TempDataDictionary(_httpContext, _tempDataProvider.Object),
            Input = new ChangeUserNameInputModel { UserName = userName }
        };
    }

    private async Task<string?> GetStoredUserNameAsync(User user) {
        await using var scope = _rootProvider!.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExtendedIdentityDbContext<User, Role>>();
        return await dbContext.Users.Where(x => x.Id == user.Id).Select(x => x.UserName).SingleAsync();
    }

    [Fact]
    public async Task OnGet_Shows_Mfa_Challenge_When_Required() {
        await SetupAsync();
        _status = ProfileMfaChallengeStatus.Required;
        var page = CreatePage();

        var result = await page.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.NotNull(page.MfaChallenge);
        Assert.Equal(CurrentUserName, page.CurrentUserName);
    }

    [Fact]
    public async Task OnGet_Does_Not_Show_Mfa_Challenge_When_Mfa_Is_Not_Required() {
        await SetupAsync();
        var page = CreatePage();

        var result = await page.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Null(page.MfaChallenge);
    }

    [Theory]
    [InlineData(ProfileMfaChallengeStatus.Required)]
    [InlineData(ProfileMfaChallengeStatus.Blocked)]
    public async Task OnPost_Does_Not_Change_UserName_Until_Mfa_Challenge_Is_Satisfied(ProfileMfaChallengeStatus status) {
        var user = await SetupAsync();
        _status = status;
        var page = CreatePage("someone-else");

        var result = await page.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.NotNull(page.MfaChallenge);
        Assert.Equal(status == ProfileMfaChallengeStatus.Blocked, page.MfaChallenge.IsBlocked);
        Assert.Equal(CurrentUserName, await GetStoredUserNameAsync(user));
    }

    [Theory]
    [InlineData(ProfileMfaChallengeStatus.Satisfied)]
    [InlineData(ProfileMfaChallengeStatus.NotRequired)]
    public async Task OnPost_Changes_UserName_When_Allowed(ProfileMfaChallengeStatus status) {
        var user = await SetupAsync();
        _status = status;
        var page = CreatePage("someone-else");

        var result = await page.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Profile", redirect.PageName);
        Assert.Equal("someone-else", await GetStoredUserNameAsync(user));
        Assert.True(page.TempData.ContainsKey("Alert"));
    }

    [Fact]
    public async Task OnPost_Returns_Error_When_UserName_Is_Taken() {
        var user = await SetupAsync();
        Assert.True((await UserManager.CreateAsync(new User { UserName = "taken", Email = "taken@somewhere.com" })).Succeeded);
        var page = CreatePage("taken");

        var result = await page.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(page.ModelState.IsValid);
        Assert.True(page.ModelState.ContainsKey($"{nameof(page.Input)}.{nameof(ChangeUserNameInputModel.UserName)}"));
        Assert.Equal(CurrentUserName, await GetStoredUserNameAsync(user));
    }

    [Fact]
    public async Task Page_Is_Disabled_When_Email_Is_Used_As_UserName() {
        await SetupAsync(emailAsUserName: true);

        var get = await CreatePage().OnGetAsync();
        var post = await CreatePage("someone-else").OnPostAsync();
        var send = await CreatePage().OnPostMfaChallengeSendAsync(null);

        Assert.Equal("/Profile", Assert.IsType<RedirectToPageResult>(get).PageName);
        Assert.IsType<BadRequestResult>(post);
        Assert.IsType<BadRequestResult>(send);
        _mfaChallengeService.Verify(x => x.SendCodeAsync(It.IsAny<User>(), It.IsAny<AuthenticationMethod>()), Times.Never);
    }

    [Fact]
    public async Task Page_Is_Disabled_When_Email_Edit_Is_Disabled() {
        var user = await SetupAsync();
        _uiOptions.DisableEmailEdit = true;

        var get = await CreatePage().OnGetAsync();
        var post = await CreatePage("someone-else").OnPostAsync();

        Assert.Equal("/Profile", Assert.IsType<RedirectToPageResult>(get).PageName);
        Assert.IsType<BadRequestResult>(post);
        Assert.Equal(CurrentUserName, await GetStoredUserNameAsync(user));
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() {
        await _scope.DisposeAsync();
        if (_rootProvider is not null) {
            await _rootProvider.DisposeAsync();
        }
    }

    private sealed class TestChangeUserNameModel(ExtendedUserManager<User> userManager, IOptions<IdentityUIOptions> identityUiOptions) : BaseChangeUserNameModel(userManager, identityUiOptions);

    private sealed class TestHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }
}
