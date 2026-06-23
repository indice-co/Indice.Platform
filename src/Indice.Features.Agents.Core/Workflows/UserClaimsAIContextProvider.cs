using System.Security.Claims;
using System.Text;
using Indice.Features.Agents.Core.Services;
using Indice.Security;
using Microsoft.Agents.AI;

namespace Indice.Features.Agents.Core.Workflows;

/// <summary>
/// Delegate that returns the current <see cref="ClaimsPrincipal"/> for the workflow invocation. The delegate is
/// invoked lazily inside <c>ProvideAIContextAsync</c>, so a single provider instance is safe to attach to any agent.
/// </summary>
/// <returns>The current <see cref="ClaimsPrincipal"/> or <c>null</c> if no user is authenticated.</returns>
public delegate ClaimsPrincipal? WorkflowClaimsPrincipalSelector();

/// <summary>
/// Contributes the caller's profile as additional instructions on every agent invocation. Reads the ambient
/// <see cref="ClaimsPrincipal"/> (name / gender / locale) via <see cref="WorkflowClaimsPrincipalSelector"/> and augments
/// it with the application-local profile (<see cref="IUsersService"/>): a stored preferred language and response
/// style override/extend the claim snapshot. Resolves both lazily inside <c>ProvideAIContextAsync</c>, so a
/// single provider instance is safe to attach to any agent. Returns an empty <see cref="AIContext"/> when the
/// request is anonymous or nothing personalizable is present; falls back to claims when no profile row exists.
/// </summary>
public sealed class UserClaimsAIContextProvider : AIContextProvider {
    private readonly WorkflowClaimsPrincipalSelector _claimsPrincipalSelector;
    private readonly IUsersService _usersService;

    /// <summary>Creates a new <see cref="UserClaimsAIContextProvider"/>.</summary>
    public UserClaimsAIContextProvider(WorkflowClaimsPrincipalSelector claimsPrincipalSelector, IUsersService usersService) : base(null, null) {
        _claimsPrincipalSelector = claimsPrincipalSelector;
        _usersService = usersService;
    }

    /// <inheritdoc/>
    protected override async ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default) {
        var principal = _claimsPrincipalSelector();
        if (principal is null || principal?.Identity?.IsAuthenticated != true) {
            return new AIContext();
        }
        var gender = principal.FindFirst(BasicClaimTypes.Gender)?.Value;
        var locale = principal.FindFirst(BasicClaimTypes.Locale)?.Value;

        // Augment the claim snapshot with the stored profile; the preferences own the personalization knobs.
        var subjectId = principal.FindSubjectId();
        var profile = subjectId is null ? null : await _usersService.GetAsync(subjectId, cancellationToken);
        var name = profile?.DisplayName ?? principal.FindFirst(BasicClaimTypes.Name)?.Value;
        var preferredLanguage = profile?.PreferredLanguage;
        var responseStyle = profile?.ResponseStyle;

        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(gender) && string.IsNullOrWhiteSpace(locale)
            && string.IsNullOrWhiteSpace(preferredLanguage) && string.IsNullOrWhiteSpace(responseStyle)) {
            return new AIContext();
        }
        var sb = new StringBuilder();
        sb.AppendLine("USER PROFILE:");
        if (!string.IsNullOrWhiteSpace(name))   sb.Append("- Name: ").AppendLine(name);
        if (!string.IsNullOrWhiteSpace(gender)) sb.Append("- Gender: ").AppendLine(gender);
        if (!string.IsNullOrWhiteSpace(locale)) sb.Append("- Locale: ").AppendLine(locale);
        sb.Append("Use this to personalize tone where appropriate; respect locale for date/number formats; do not assume gender pronouns when gender is absent.");
        if (!string.IsNullOrWhiteSpace(preferredLanguage)) {
            sb.AppendLine().Append("Always write the answer in this language: ").Append(preferredLanguage).Append('.');
        }
        if (!string.IsNullOrWhiteSpace(responseStyle)) {
            sb.AppendLine().Append("Preferred response style: ").Append(responseStyle).Append(" — adapt verbosity and tone accordingly.");
        }
        return new AIContext { Instructions = sb.ToString() };
    }
}
