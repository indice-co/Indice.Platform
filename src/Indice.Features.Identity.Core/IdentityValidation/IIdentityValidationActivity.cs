using System.Globalization;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Identity.Core.IdentityValidation;

/// <summary>
/// Represents an activity that will take place after the successful first authentication of the user 
/// but before releasing and completing the signin process. 
/// These activities are meant for the user to complete any pending tasks regarding his identity so that after
/// The signin process completes we end up with a valid identity.
/// Examples of such activities are MFA, EmailConfirmation, PhoneConfirmation, PasswordChange, MFAOnBoarding, TermsAcceptance.
/// </summary>
/// <remarks>These activities will determine the flow that the user must take. They are implemented as a pipeline of filters.</remarks>
public interface IIdentityValidationActivity
{
    /// <summary>
    /// The next activity in the pipeline.
    /// </summary>
    public IIdentityValidationActivity? Next { get; set; }
    /// <summary>
    /// When called the handle method will try to handle the incoming request. 
    /// If it is not meant for this Activity then it will pass it to the next.
    /// </summary>
    /// <param name="activityContext">The input state to check for handler compatibility</param>
    /// <returns></returns>
    Task HandleAsync(UserValidationActivityContext activityContext);
}

/// <summary>
/// The request context for the filter chain of Validation activities. <seealso cref="IIdentityValidationActivity"/>.
/// </summary>
public class UserValidationActivityContext(User user, HttpContext httpContext)
{
    /// <summary>The user to validate</summary>
    public User User { get; } = user;
    /// <summary>The current httpcontext</summary>
    public HttpContext HttpContext { get; } = httpContext;
    /// <summary>Represents the result of an identity validation activity operation.</summary>
    public UserValidationRequirement? Result { get; set; }
};

/// <summary>
/// Abstract class that encapsulates an <see cref="IIdentityValidationActivity"/>
/// </summary>
public abstract class IdentityValidationActivityBase : IIdentityValidationActivity
{

    /// <inheritdoc/>
    public IIdentityValidationActivity? Next { get; set; }

    /// <inheritdoc/>
    public async Task HandleAsync(UserValidationActivityContext activityContext) {
        activityContext.Result = await GetResultAsync(activityContext);
        if (activityContext.Result is not null) {
            return;
        }
        if (Next is not null) {
            await Next.HandleAsync(activityContext);
        }
    }

    /// <summary>
    /// Should return a valid result if activitiy is handled otherwize null so that the chain can continue.
    /// </summary>
    /// <param name="activityContext"></param>
    /// <returns></returns>
    protected abstract ValueTask<UserValidationRequirement?> GetResultAsync(UserValidationActivityContext activityContext);
}

/// <summary>
/// Activity handler for determine the need for mfa onboarding
/// </summary>
public class RequiresMfaOnboardingActivity : IdentityValidationActivityBase
{
    /// <inheritdoc/>
    protected override async ValueTask<UserValidationRequirement?> GetResultAsync(UserValidationActivityContext activityContext) {
        await ValueTask.CompletedTask;
        var configuration = activityContext.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var mfaPolicy = activityContext.User.TwoFactorPolicy ?? configuration.GetIdentityOption<MfaPolicy>($"{nameof(IdentityOptions.SignIn)}:Mfa", "Policy");

        if (activityContext.User.TwoFactorEnabled == false && mfaPolicy == MfaPolicy.Enforced) {
            return new UserValidationRequirement(UserActivityRequirementKind.RequiresMfaOnboarding, "/MfaOnboarding");
        }
        return null;
    }
}

/// <summary>
/// Activity handler for determine the need for Password Change
/// </summary>
public class RequiresPasswordChangeActivity : IdentityValidationActivityBase
{
    /// <inheritdoc/>
    protected override async ValueTask<UserValidationRequirement?> GetResultAsync(UserValidationActivityContext activityContext) {
        await ValueTask.CompletedTask;
        if (activityContext.User.HasExpiredPassword()) {
            return new UserValidationRequirement(UserActivityRequirementKind.RequiresPasswordChange, "/PasswordExpired");
        }
        return null;
    }
}

/// <summary>
/// Activity handler for determine the need for Email Verification
/// </summary>
public class RequiresEmailVerificationActivity : IdentityValidationActivityBase
{
    /// <inheritdoc/>
    protected override async ValueTask<UserValidationRequirement?> GetResultAsync(UserValidationActivityContext activityContext) {
        await ValueTask.CompletedTask;
        var configuration = activityContext.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var requirePostSignInConfirmedEmail = configuration.GetIdentityOption<bool>(nameof(IdentityOptions.SignIn), nameof(ExtendedSignInManager<User>.RequirePostSignInConfirmedEmail));

        if (activityContext.User.EmailConfirmed == false && requirePostSignInConfirmedEmail) {
            return new UserValidationRequirement(UserActivityRequirementKind.RequiresEmailVerification, "/AddEmail");
        }
        return null;
    }
}

/// <summary>
/// Activity handler for determine the need for Phone Verification
/// </summary>
public class RequiresPhoneNumberVerificationActivity : IdentityValidationActivityBase
{
    /// <inheritdoc/>
    protected override async ValueTask<UserValidationRequirement?> GetResultAsync(UserValidationActivityContext activityContext) {
        await ValueTask.CompletedTask;
        var configuration = activityContext.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var requirePostSignInConfirmedPhoneNumber = configuration.GetIdentityOption<bool>(nameof(IdentityOptions.SignIn), nameof(ExtendedSignInManager<User>.RequirePostSignInConfirmedPhoneNumber));

        if (activityContext.User.PhoneNumberConfirmed == false && requirePostSignInConfirmedPhoneNumber) {
            return new UserValidationRequirement(UserActivityRequirementKind.RequiresPhoneNumberVerification, "/AddPhone");
        }
        return null;
    }
}

/// <summary>
/// Activity handler for determine the need for Phone Verification
/// </summary>
public class RequiresTermsAcceptanceActivity : IdentityValidationActivityBase
{
    /// <inheritdoc/>
    protected override async ValueTask<UserValidationRequirement?> GetResultAsync(UserValidationActivityContext activityContext) {
        await ValueTask.CompletedTask;

        var configuration = activityContext.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var requirePostSignInAcceptedTerms = configuration.GetIdentityOption<bool>(nameof(IdentityOptions.SignIn), nameof(ExtendedSignInManager<User>.RequirePostSignInAcceptedTerms));
        var latestTermsRelease = configuration.GetIdentityOption<string?>(nameof(IdentityOptions.SignIn), nameof(ExtendedSignInManager<User>.LatestTermsReleaseDate));
        var latestTermsReleaseDate = DateTime.MinValue;
        if (!string.IsNullOrEmpty(latestTermsRelease))
            DateTime.TryParseExact(latestTermsRelease, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out latestTermsReleaseDate);

        var hasAcceptedTerms = activityContext.User.Claims.Where(x => x.ClaimType == BasicClaimTypes.ConsentTerms).Select(x => bool.TrueString.Equals(x.ClaimValue, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        var acceptedTermsDate = activityContext.User.Claims
            .Where(x => x.ClaimType == BasicClaimTypes.ConsentTermsDate)
            .Select(x => string.IsNullOrEmpty(x.ClaimValue)
                ? DateTime.MinValue
                : (DateTime.TryParseExact(x.ClaimValue, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate)
                    ? parsedDate
                    : DateTime.MinValue))
            .FirstOrDefault();

        if (requirePostSignInAcceptedTerms &&
            (!hasAcceptedTerms ||
            (latestTermsReleaseDate > DateTime.MinValue && acceptedTermsDate < latestTermsReleaseDate))) {
            return new UserValidationRequirement(UserActivityRequirementKind.RequiresAcceptanceOfTerms, "/AcceptTerms");
        }
        return null;
    }
}
