using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Indice.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Validator for <see cref="CreateClaimTypeRequest"/> model.
    /// </summary>
    public class CreateClaimTypeRequestValidator : AbstractValidator<CreateClaimTypeRequest>
    {
        private readonly Func<ExtendedIdentityDbContext> _getDbContext;

        /// <summary>
        /// Creates a new instance of <see cref="CreateClaimTypeRequestValidator"/>.
        /// </summary>
        public CreateClaimTypeRequestValidator(Func<ExtendedIdentityDbContext> getDbContext) {
            _getDbContext = getDbContext ?? throw new ArgumentNullException(nameof(getDbContext));
            RuleFor(x => x.Description)
                .MaximumLength(TextSizePresets.L1024)
                .WithMessage($"Claim name cannot be greater than {TextSizePresets.L1024} characters.");
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Claim name cannot be empty.")
                .MaximumLength(TextSizePresets.S64)
                .WithMessage($"Claim name cannot be greater than {TextSizePresets.S64} characters.")
                .MustAsync(HaveUniqueName)
                .WithMessage("Claim with name '{PropertyValue}' already exists.");
            RuleFor(x => x.DisplayName)
                .MaximumLength(TextSizePresets.M128)
                .WithMessage($"Claim display name cannot be greater than {TextSizePresets.M128} characters.");
            RuleFor(x => x.Rule)
                .RegularExpression()
                .WithMessage("Please provider a valid regular expression.")
                .When(x => !string.IsNullOrEmpty(x.Rule));
        }

        private async Task<bool> HaveUniqueName(string name, CancellationToken cancellationToken) {
            var dbContext = _getDbContext();
            var exists = await dbContext.ClaimTypes.AsNoTracking().AnyAsync(x => x.Name == name, cancellationToken);
            return !exists;
        }
    }

    /// <summary>
    /// Validator for <see cref="UpdateClaimTypeRequest"/> model.
    /// </summary>
    public class UpdateClaimTypeRequestValidator : AbstractValidator<UpdateClaimTypeRequest>
    {
        /// <summary>
        /// Creates a new instance of <see cref="UpdateClaimTypeRequestValidator"/>.
        /// </summary>
        public UpdateClaimTypeRequestValidator(Func<ExtendedIdentityDbContext> getDbContext) {
            RuleFor(x => x.Description)
                .MaximumLength(TextSizePresets.L1024)
                .WithMessage($"Claim name cannot be greater than {TextSizePresets.L1024} characters.");
            RuleFor(x => x.DisplayName)
                .MaximumLength(TextSizePresets.M128)
                .WithMessage($"Claim display name cannot be greater than {TextSizePresets.M128} characters.");
            RuleFor(x => x.Rule)
                .RegularExpression()
                .When(x => !string.IsNullOrEmpty(x.Rule))
                .WithMessage("Please provider a valid regular expression.");
        }
    }
}
