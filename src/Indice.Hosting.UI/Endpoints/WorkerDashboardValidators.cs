using FluentValidation;
using Indice.Hosting.UI.Models;

namespace Indice.Hosting.UI.Endpoints;

/// <summary>Validates <see cref="MessageIdsRequest"/>. Wired via <c>WithParameterValidation&lt;MessageIdsRequest&gt;()</c>.</summary>
public class MessageIdsRequestValidator : AbstractValidator<MessageIdsRequest>
{
    /// <summary>The maximum number of messages in one bulk action.</summary>
    public const int MaxIds = 1000;

    /// <summary>Creates a new <see cref="MessageIdsRequestValidator"/>.</summary>
    public MessageIdsRequestValidator() {
        RuleFor(x => x.Ids)
            .NotEmpty()
            .Must(ids => ids.Length <= MaxIds)
            .WithMessage($"A bulk action cannot contain more than {MaxIds} messages.");
    }
}
