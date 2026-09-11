using FluentValidation;
using Indice.Features.Agents.Core.Models;

namespace Indice.Features.Agents.Server.Endpoints;

/// <summary>Validates <see cref="ChatRequest"/>. Wired via <c>WithParameterValidation&lt;ChatRequest&gt;()</c>.</summary>
public class ChatRequestValidator : AbstractValidator<ChatRequest>
{
    /// <summary>Creates a new <see cref="ChatRequestValidator"/>.</summary>
    public ChatRequestValidator() {
        RuleFor(x => x.Text)
            .NotEmpty()
            .MinimumLength(1)
            .MaximumLength(2000);

        RuleFor(x => x.AuthorName)
            .MaximumLength(250);

        RuleFor(x => x.AgentName)
            .MaximumLength(250);

        // Runs only when Topic is not null; null values are skipped automatically.
        RuleFor(x => x.Topic!)
            .SetValidator(new ChatTopicValidator());
    }
}

/// <summary>Validates <see cref="ChatTopic"/>.</summary>
public class ChatTopicValidator : AbstractValidator<ChatTopic>
{
    /// <summary>Creates a new <see cref="ChatTopicValidator"/>.</summary>
    public ChatTopicValidator() {
        RuleFor(x => x.ReferenceId)
            .MaximumLength(100);
        RuleFor(x => x.ReferenceType)
            .MaximumLength(250);
    }
}