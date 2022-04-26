using FluentValidation;
using Indice.Configuration;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Messages.Core.Services.Validators
{
    /// <summary>Contains validation logic for <see cref="UpsertMessageTypeRequest"/>.</summary>
    public class UpsertMessageTypeRequestValidator : AbstractValidator<UpsertMessageTypeRequest>
    {
        /// <summary>Creates a new instance of <see cref="UpsertMessageTypeRequestValidator"/>.</summary>
        public UpsertMessageTypeRequestValidator(IServiceProvider serviceProvider) {
            var messageTypeService = serviceProvider.GetRequiredService<IMessageTypeService>();
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Please provide a name for the campaign type.")
                .MaximumLength(TextSizePresets.M128)
                .WithMessage($"Maximum length for name is {TextSizePresets.M128} characters.")
                .MustAsync(async (name, cancellationToken) => string.IsNullOrWhiteSpace(name) ? true : await messageTypeService.GetByName(name) == null)
                .WithMessage(x => $"There is already a campaign type with name '{x.Name}'.");
        }
    }
}
