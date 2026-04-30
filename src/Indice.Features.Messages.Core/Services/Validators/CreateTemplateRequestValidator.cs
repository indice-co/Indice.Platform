using System;
using FluentValidation;
using Indice.Configuration;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Messages.Core.Services.Validators;

/// <summary>Contains validation logic for <see cref="CreateTemplateRequest"/>.</summary>
public class CreateTemplateRequestValidator : AbstractValidator<CreateTemplateRequest>
{
    /// <summary>Creates a new instance of <see cref="CreateTemplateRequestValidator"/>.</summary>
    public CreateTemplateRequestValidator(IServiceProvider serviceProvider) {
        var templateService = serviceProvider.GetRequiredService<ITemplateService>();
        RuleFor(x => x.Content)
            .Must(x => x.Count > 0).WithMessage("Please specify content for the template.")
            .ForEach(ruleBuilder => {
                ruleBuilder.Must(BeValidChannelKind)
                           .WithMessage((collection, pair) => $"Channel '{pair.Key}' is not valid.")
                           .Must(kvp => !BeValidChannelKind(kvp) || (!string.IsNullOrWhiteSpace(kvp.Value?.Title) && !string.IsNullOrWhiteSpace(kvp.Value?.Body)))
                           .WithMessage((collection, pair) => $"Channel '{pair.Key}' must have it's title and body specified.");
            });

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Please provide a name for the template.")
            .MaximumLength(TextSizePresets.M256)
            .WithMessage($"Maximum length for name is {TextSizePresets.M256} characters.")
            .Must(name => !templateService.ExistsByName(name).Result)
            .WithMessage(x => $"There is already a template with name '{x.Name}'.");

        RuleFor(x => x.Alias)
            .MaximumLength(TextSizePresets.S64)
            .WithMessage($"Maximum length for alias is {TextSizePresets.S64} characters.")
            .MustAsync(async (alias, ct) => {
                var aliasIsEmpty = string.IsNullOrWhiteSpace(alias);
                var aliasExists = !string.IsNullOrWhiteSpace(alias) && (await templateService.GetById((GuidOrAlias)alias)) is null;
                return aliasIsEmpty || aliasExists;
            })
            .WithMessage(x => $"There is already a template with the same alias '{x.Alias}'.");
        RuleFor(x => x.Type)
            .Must(BeValidTemplateType)
            .WithMessage(x => $"The template type '{x.Type}' is not valid.");
    }



    private static bool BeValidChannelKind(KeyValuePair<string, MessageContent> kvp) => Enum.TryParse(typeof(MessageChannelKind), kvp.Key, ignoreCase: true, out var _);
    private static bool BeValidTemplateType(TemplateType type) => Enum.IsDefined(typeof(TemplateType), type);

}
