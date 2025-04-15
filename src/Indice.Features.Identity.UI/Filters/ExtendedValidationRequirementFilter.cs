using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Identity.UI.Filters;
/// <summary>
/// A filter that is used to enforce the extended validation state machine the extended validation scheme.
/// </summary>
/// <typeparam name="TUser"></typeparam>
public class ExtendedValidationRequirementFilter<TUser> : ResultFilterAttribute where TUser : User
{
    /// <summary>
    /// Creates a new instance of <see cref="ExtendedValidationRequirementFilter{TUser}"/> class.
    /// </summary>
    /// <param name="requirement">The user requirement that is associated to the executing page.</param>
    /// <param name="autoRedirect">Auto redirect to next state</param>
    public ExtendedValidationRequirementFilter(UserActivityRequirementKind requirement, bool autoRedirect = false) {
        RequirementKind = requirement;
        AutoRedirect = autoRedirect;
    }

    /// <summary>
    /// The user state that is associated to the executing page.
    /// </summary>
    /// <remarks>This is used to guard against manual overriding the current flow by changing the page url.</remarks>
    public UserActivityRequirementKind RequirementKind { get; }
    /// <summary>
    /// Auto redirect to next state.
    /// </summary>
    public bool AutoRedirect { get; }

    /// <inheritdoc/>
    public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next) {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Result is not PageResult pageResult || pageResult?.Model is not BasePageModel pageModel) {
            await base.OnResultExecutionAsync(context, next);
            return;
        }
        var userManager = context.HttpContext.RequestServices.GetRequiredService<ExtendedUserManager<TUser>>();
        var user = await userManager.GetUserAsync(context.HttpContext.User);
        if (user is null) {
            throw new InvalidOperationException("user not found");
        }
        await CheckStateAndRedirectAsync(context, pageResult, pageModel, user);
        await base.OnResultExecutionAsync(context, next);
    }

    private async Task CheckStateAndRedirectAsync(ResultExecutingContext context, PageResult pageResult, BasePageModel pageModel, TUser user) {
        var userStateProvider = context.HttpContext.RequestServices.GetRequiredService<IUserRequirementProvider<TUser>>();
        var signInManager = context.HttpContext.RequestServices.GetRequiredService<ExtendedSignInManager<TUser>>();
        var requirement = await userStateProvider.GetNextAsync(context.HttpContext, user);
        var returnUrl = context.HttpContext.Request.Query["returnUrl"].ToString();
        if (requirement == UserValidationRequirement.None) {
            await signInManager.AutoSignIn(user, ExtendedIdentityConstants.ExtendedValidationScheme);
            context.Result = pageModel.IsValidReturnUrl(returnUrl) ? new RedirectResult(returnUrl) : new RedirectResult("/");
            return;
        }
        if (requirement.Kind != RequirementKind) {
            context.Result = new RedirectResult(pageModel.Url.PageLink(requirement.PageName, values: new { returnUrl }) ?? "/");
            return;
        }
    }
}
