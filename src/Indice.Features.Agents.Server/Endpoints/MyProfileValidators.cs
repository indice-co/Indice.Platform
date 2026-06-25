using FluentValidation;

namespace Indice.Features.Agents.Server.Endpoints;

/// <summary>
/// The config-driven <c>PreferredLanguage</c>-against-taxonomy check lives in <c>MyProfileService</c>
/// (it needs runtime <see cref="Core.AgentsOptions"/>), keeping this validator dependency-free.
/// </summary>
public class MyProfileValidators : AbstractValidator<UpdateUserRequest>
{
    /// <summary>Allowed response styles fed to the composer.</summary>
    public static readonly string[] AllowedResponseStyles = ["concise", "detailed", "formal"];

    /// <summary>Creates a new <see cref="MyProfileValidators"/>.</summary>
    public MyProfileValidators() {
        RuleFor(x => x.PreferredLanguage)
            .MaximumLength(16)
            .When(x => !string.IsNullOrWhiteSpace(x.PreferredLanguage));
        RuleFor(x => x.ResponseStyle)
            .Must(style => AllowedResponseStyles.Contains(style!))
            .When(x => !string.IsNullOrWhiteSpace(x.ResponseStyle))
            .WithMessage($"ResponseStyle must be one of: {string.Join(", ", AllowedResponseStyles)}.");
    }
}
