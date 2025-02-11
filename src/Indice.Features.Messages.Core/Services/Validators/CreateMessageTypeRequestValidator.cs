using FluentValidation;
using Indice.Configuration;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Messages.Core.Services.Validators;

/// <summary>Contains validation logic for <see cref="CreateMessageTypeRequest"/>.</summary>
public class CreateMessageTypeRequestValidator : AbstractValidator<CreateMessageTypeRequest>
{
    /// <summary>Creates a new instance of <see cref="CreateMessageTypeRequestValidator"/>.</summary>
    public CreateMessageTypeRequestValidator(IServiceProvider serviceProvider) {
        var messageTypeService = serviceProvider.GetRequiredService<IMessageTypeService>();
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Please provide a name for the campaign type.")
            .MaximumLength(TextSizePresets.M128)
            .WithMessage($"Maximum length for name is {TextSizePresets.M128} characters.")
            .MustAsync(async (name, ctx) => {
                return string.IsNullOrWhiteSpace(name) || (await messageTypeService.GetByName(name)) is null;
            })
            .WithMessage(x => $"There is already a campaign type with name '{x.Name}'.");

        RuleFor(x => x.Alias)
            .MaximumLength(TextSizePresets.S64)
            .WithMessage($"Maximum length for alias is {TextSizePresets.S64} characters.")
            .MustAsync(async (alias, ctx) => {
                var aliasIsEmpty = string.IsNullOrWhiteSpace(alias);
                var aliasExists = !string.IsNullOrWhiteSpace(alias) && (await messageTypeService.GetById((GuidOrAlias)alias)) is null;
                return aliasIsEmpty || aliasExists;
            })
            .WithMessage(x => $"There is already a campaign type with the same alias '{x.Alias}'.");
    }
}
