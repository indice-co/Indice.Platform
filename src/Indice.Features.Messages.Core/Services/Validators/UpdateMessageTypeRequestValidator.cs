using FluentValidation;
using Indice.Configuration;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Messages.Core.Services.Validators;

/// <summary>Contains validation logic for <see cref="UpdateMessageTypeRequest"/>.</summary>
public class UpdateMessageTypeRequestValidator : AbstractValidator<UpdateMessageTypeRequest>
{
    /// <summary>Creates a new instance of <see cref="UpdateMessageTypeRequestValidator"/>.</summary>
    public UpdateMessageTypeRequestValidator(IServiceProvider serviceProvider) {
        var messageTypeService = serviceProvider.GetRequiredService<IMessageTypeService>();
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Please provide a name for the campaign type.")
            .MaximumLength(TextSizePresets.M128)
            .WithMessage($"Maximum length for name is {TextSizePresets.M128} characters.");
    }
}
