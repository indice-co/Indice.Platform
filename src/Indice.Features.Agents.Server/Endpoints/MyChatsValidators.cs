using System.Net.Mime;
using System.Text.Json;
using FluentValidation;
using Indice.Extensions;
using Indice.Features.Agents.Core.Models;
using Indice.Services;

namespace Indice.Features.Agents.Server.Endpoints;

/// <summary>Validates <see cref="ChatRequest"/>. Wired via <c>WithParameterValidation&lt;ChatRequest&gt;()</c>.</summary>
public class ChatRequestValidator : AbstractValidator<ChatRequest>
{
    private const int MaxParts = 5;

    /// <summary>Creates a new <see cref="ChatRequestValidator"/>.</summary>
    public ChatRequestValidator(IMagicBytesValidator magicBytesValidator) {
        RuleFor(x => x.Text)
            .NotEmpty()
            .MinimumLength(1)
            .MaximumLength(2000);

        RuleFor(x => x.AuthorName)
            .MaximumLength(250);

        RuleFor(x => x.AgentName)
            .MaximumLength(250);

        RuleFor(x => x.Parts)
            .Must(parts => parts.Count <= MaxParts)
            .WithMessage($"A chat request cannot contain more than {MaxParts} parts.")
            .Must(parts => parts.Count(part => part.ContentType?.StartsWith("text/", StringComparison.OrdinalIgnoreCase) == true) <= 1)
            .WithMessage("A chat request cannot contain more than one text part.");

        RuleForEach(x => x.Parts)
            .SetValidator(new ChatMessagePartValidator(magicBytesValidator));

        // Runs only when Topic is not null; null values are skipped automatically.
        RuleFor(x => x.Topic!)
            .SetValidator(new ChatTopicValidator());
    }
}

/// <summary>Validates <see cref="ChatMessagePart"/>.</summary>
public class ChatMessagePartValidator : AbstractValidator<ChatMessagePart>
{
    private const int MaxNonTextValueLength = 2 * 1024 * 1024;
    private readonly IMagicBytesValidator _magicBytesValidator;

    /// <summary>Creates a new <see cref="ChatMessagePartValidator"/>.</summary>
    public ChatMessagePartValidator(IMagicBytesValidator magicBytesValidator) {
        _magicBytesValidator = magicBytesValidator;

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(BeAllowedContentType)
            .WithMessage("Content type '{PropertyValue}' is not allowed. Allowed content types are 'text/*', 'image/*', 'application/json' and 'application/vnd.indice*+json'.");

        RuleFor(x => x.Name).MaximumLength(250);

        RuleFor(x => x.Value)
            .NotNull();

        When(x => !IsText(x.ContentType), () => {
            RuleFor(x => x.Value)
                .MaximumLength(MaxNonTextValueLength);
        });

        When(x => IsImage(x.ContentType), () => {
            RuleFor(x => x.Value)
                .MustAsync((part, value, cancellationToken) => BeValidImage(part.ContentType, value, cancellationToken))
                .WithMessage("The image content does not match the declared content type or the image format is not supported.");
        });

        When(x => IsJson(x.ContentType), () => {
            RuleFor(x => x.Value)
                .Must(BeValidJson)
                .WithMessage("The value is not valid JSON.");
        });
    }

    private static bool IsText(string? contentType) => contentType?.StartsWith("text/", StringComparison.OrdinalIgnoreCase) == true;
    private static bool IsImage(string? contentType) => contentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true;
    private static bool IsAllowedImage(string? contentType) => contentType is not null && 
                                                               (contentType.Equals(MediaTypeNames.Image.Png, StringComparison.OrdinalIgnoreCase) ||
                                                                contentType.Equals(MediaTypeNames.Image.Jpeg, StringComparison.OrdinalIgnoreCase));
    private static bool IsJson(string? contentType) =>
        string.Equals(contentType, MediaTypeNames.Application.Json, StringComparison.OrdinalIgnoreCase) ||
        (contentType?.StartsWith("application/vnd.indice", StringComparison.OrdinalIgnoreCase) == true && contentType.EndsWith("+json", StringComparison.OrdinalIgnoreCase));

    private static bool BeAllowedContentType(string contentType) =>
        IsText(contentType) || IsAllowedImage(contentType) || IsJson(contentType);

    private static bool BeValidJson(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return false;
        }
        try {
            using var _ = JsonDocument.Parse(value);
            return true;
        } catch (JsonException) {
            return false;
        }
    }

    private async Task<bool> BeValidImage(string contentType, string? value, CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(value)) {
            return false;
        }
        if (!FileExtensions.TryGetFileExtension(contentType, out var extension)) {
            return false;
        }
        if (!TryGetDataUriBytes(value, out var bytes) || bytes!.Length == 0) {
            return false;
        }
        using var stream = new MemoryStream(bytes, writable: false);
        var result = await _magicBytesValidator.IsValid(stream, extension!, cancellationToken);
        return result.IsValid;
    }

    private static bool TryGetDataUriBytes(string value, out byte[]? bytes) {
        bytes = null;
        if (!value.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) {
            return false;
        }
        var commaIndex = value.IndexOf(',');
        if (commaIndex < 0 || !value[..commaIndex].EndsWith(";base64", StringComparison.OrdinalIgnoreCase)) {
            return false;
        }
        var payload = value[(commaIndex + 1)..];
        var buffer = new byte[(payload.Length * 3 / 4) + 3];
        if (!Convert.TryFromBase64String(payload, buffer, out var bytesWritten)) {
            return false;
        }
        bytes = buffer[..bytesWritten];
        return true;
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