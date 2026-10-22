using FluentValidation;
using Indice.Features.Workers.Server.Models;

namespace Indice.Features.Workers.Server.Endpoints;

/// <summary>Validates <see cref="MessageIdsRequest"/>.</summary>
public class MessageIdsRequestValidator : AbstractValidator<MessageIdsRequest>
{
    // todo: check here if and what value needed
    // todo: requeue by queue name might also be better here as the usual Officer action
    /// <summary>The maximum number of messages in one bulk action.</summary>
    private const int MaxIds = 1000;

    /// <summary>Creates a new <see cref="MessageIdsRequestValidator"/>.</summary>
    public MessageIdsRequestValidator() {
        RuleFor(x => x.Ids)
            .NotEmpty()
            .Must(ids => ids is null || ids.Length <= MaxIds)
            .WithMessage($"A bulk action cannot contain more than {MaxIds} messages.");
    }
}
