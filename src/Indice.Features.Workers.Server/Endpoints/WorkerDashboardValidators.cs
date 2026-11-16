using System.Text.Json;
using FluentValidation;
using Indice.Features.Workers.Server.Models;
using Microsoft.Extensions.Options;

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

/// <summary>Validates <see cref="EnqueueMessageRequest"/>.</summary>
public class EnqueueMessageRequestValidator : AbstractValidator<EnqueueMessageRequest>
{
    /// <summary>Creates a new <see cref="EnqueueMessageRequestValidator"/>.</summary>
    public EnqueueMessageRequestValidator(IOptions<WorkerEndpointsOptions> options) {
        RuleFor(x => x.Payload).ValidJsonPayload(options.Value.MaxPayloadLength);
    }
}

/// <summary>Validates <see cref="UpdateMessageRequest"/>.</summary>
public class UpdateMessageRequestValidator : AbstractValidator<UpdateMessageRequest>
{
    /// <summary>Creates a new <see cref="UpdateMessageRequestValidator"/>.</summary>
    public UpdateMessageRequestValidator(IOptions<WorkerEndpointsOptions> options) {
        RuleFor(x => x.Payload).ValidJsonPayload(options.Value.MaxPayloadLength);
    }
}

internal static class PayloadValidationExtensions
{
    public static IRuleBuilderOptions<T, string> ValidJsonPayload<T>(this IRuleBuilder<T, string> rule, int maxLength) => rule
        .NotEmpty()
        .MaximumLength(maxLength)
        .Must(BeValidJson)
        .WithMessage("The payload must be valid JSON.");

    private static bool BeValidJson(string? payload) {
        if (string.IsNullOrWhiteSpace(payload)) {
            return true;
        }
        try {
            using var _ = JsonDocument.Parse(payload);
            return true;
        } catch (JsonException) {
            return false;
        }
    }
}
