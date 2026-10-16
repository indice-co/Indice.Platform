using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Agents.Core.Services.Validation;

/// <summary>
/// Resolves keyed <see cref="IChallengeValidator"/> registrations by case type,
/// falling back to the validator registered under <see cref="DefaultChallengeValidator.Default"/>.
/// </summary>
public sealed class ChallengeValidatorFactory 
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>Creates a new <see cref="ChallengeValidatorFactory"/>.</summary>
    public ChallengeValidatorFactory(IServiceProvider serviceProvider) {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc/>
    public IChallengeValidator Create(string? caseType) {
        if (!string.IsNullOrWhiteSpace(caseType)) {
            var validator = _serviceProvider.GetKeyedService<IChallengeValidator>(caseType);
            if (validator is not null) {
                return validator;
            }
        }
        return _serviceProvider.GetRequiredKeyedService<IChallengeValidator>(DefaultChallengeValidator.Default);
    }
}
