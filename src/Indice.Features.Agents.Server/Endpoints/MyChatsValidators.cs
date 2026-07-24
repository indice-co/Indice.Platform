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
    }
}
