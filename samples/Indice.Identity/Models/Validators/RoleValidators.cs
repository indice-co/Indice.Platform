using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Indice.Identity.Configuration;
using Indice.Identity.Data;
using Indice.Identity.Models.Requests;
using Microsoft.EntityFrameworkCore;

namespace Indice.Identity.Models.Validators
{
    /// <summary>
    /// Validator for <see cref="CreateRoleRequest"/> model.
    /// </summary>
    public class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
    {
        private readonly Func<ExtendedIdentityDbContext> _getDbContext;

        /// <summary>
        /// Creates a new instance of <see cref="CreateClaimTypeRequestValidator"/>.
        /// </summary>
        public CreateRoleRequestValidator(Func<ExtendedIdentityDbContext> getDbContext) {
            _getDbContext = getDbContext ?? throw new ArgumentNullException(nameof(getDbContext));
            RuleFor(x => x.Description)
                .MaximumLength(TextSizePresets.M512)
                .WithMessage($"Role name cannot be greater than {TextSizePresets.M512} characters.");
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Claim name cannot be empty.")
                .MaximumLength(TextSizePresets.S64)
                .WithMessage($"Role name cannot be greater than {TextSizePresets.S64} characters.")
                .MustAsync(HaveUniqueName)
                .WithMessage("Role with name '{PropertyValue}' already exists.");
        }

        private async Task<bool> HaveUniqueName(string name, CancellationToken cancellationToken) {
            var dbContext = _getDbContext();
            var exists = await dbContext.Roles.AsNoTracking().AnyAsync(x => x.Name == name, cancellationToken);
            return !exists;
        }
    }
}
