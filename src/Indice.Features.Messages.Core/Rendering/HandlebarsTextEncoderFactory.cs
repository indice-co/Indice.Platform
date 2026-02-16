using HandlebarsDotNet;
using Indice.Features.Messages.Core.Models;

namespace Indice.Features.Messages.Core.Rendering;

/// <summary>
/// Provides factory methods for creating instances of <see cref="ITextEncoder"/> based on the specified message channel
/// kind.
/// </summary>
/// <remarks>This static class enables the selection of an appropriate text encoder for different message delivery
/// channels, such as SMS or push notifications. It supports both strongly typed and string-based channel kind inputs,
/// allowing for flexible integration with various messaging workflows.</remarks>
public static class HandlebarsTextEncoderFactory
{
    /// <summary>
    /// Creates a new instance of the <see cref="ITextEncoder"/> according to the specified <see cref="MessageChannelKind"/>.
    /// </summary>
    /// <returns>A new instance of the <see cref="ITextEncoder"/> class.</returns>
    public static ITextEncoder Create(MessageChannelKind channelKind) => channelKind switch { 
        MessageChannelKind.SMS => new HandlebarsPlainTextEncoder(),
        MessageChannelKind.PushNotification => new HandlebarsPlainTextEncoder(),
        _ => new HtmlEncoder(),
    };

    /// <summary>
    /// Creates a new instance of the <see cref="ITextEncoder"/> class based on the provided channel kind as a string. 
    /// </summary>
    /// <param name="channelKind">The channel kind as a string.</param>
    /// <returns>A new instance of the <see cref="ITextEncoder"/> class.</returns>
    public static ITextEncoder Create(string channelKind) {
        if (Enum.TryParse<MessageChannelKind>(channelKind.Trim(), ignoreCase: true, out var parsedKind)) {
            return Create(parsedKind);
        }
        return new HtmlEncoder();
        }
    }


