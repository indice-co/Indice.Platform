using FluentValidation;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;

namespace Indice.Features.Messages.Core.Services.Validators
{
    /// <summary>Contains validation logic for <see cref="CreateTemplateRequest"/>.</summary>
    public class CreateTemplateRequestValidator : AbstractValidator<CreateTemplateRequest>
    {
        /// <summary>Creates a new instance of <see cref="CreateTemplateRequestValidator"/>.</summary>
        public CreateTemplateRequestValidator() {
            RuleFor(x => x.Content)
                .Must(x => x.Count > 0).WithMessage("Please specify content for the template.")
                .ForEach(ruleBuilder => {
                    ruleBuilder.Must(BeValidChannelKind)
                               .WithMessage((collection, pair) => $"Channel '{pair.Key}' is not valid.")
                               .Must(kvp => !BeValidChannelKind(kvp) || (!string.IsNullOrWhiteSpace(kvp.Value?.Title) && !string.IsNullOrWhiteSpace(kvp.Value?.Body)))
                               .WithMessage((collection, pair) => $"Channel '{pair.Key}' must have it's title and body specified.");
                });
        }

        private bool BeValidChannelKind(KeyValuePair<string, MessageContent> kvp) => Enum.TryParse(typeof(MessageChannelKind), kvp.Key, ignoreCase: true, out var _);
    }
}
