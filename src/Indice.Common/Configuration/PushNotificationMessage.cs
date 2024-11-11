using Indice.Types;

namespace Indice.Services;

/// <summary>Models the data that are sent in an push notification message.</summary>
public class PushNotificationMessage
{
    /// <summary>Constructs a <see cref="PushNotificationMessage"/>.</summary>
    /// <param name="title">The title of the push notification.</param>
    /// <param name="body">The body of the push notification.</param>
    /// <param name="data">The payload data that will be sent to the mobile client (not visible to the push notification Title or Message).  If the data is null then only the token will be sent as data.</param>
    /// <param name="tags">The tags of the push notification.</param>
    /// <param name="classification">The type of the push notification.</param>
    public PushNotificationMessage(string title, string? body, string? data, List<PushNotificationTag>? tags, string? classification) {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Body = body;
        Data = data;
        Tags = tags ?? [];
        Classification = classification;
    }

    /// <summary>
    /// The payload data that will be sent to the mobile client (not visible to the push notification Title or Message).
    /// If the data is null then only the token will be sent as data.
    /// </summary>
    public string? Data { get; }
    /// <summary>The title of the push notification.</summary>
    public string Title { get; }
    /// <summary>The title of the push notification.</summary>
    public string? Body { get; }
    /// <summary>The tags of the push notification.</summary>
    public List<PushNotificationTag> Tags { get; }
    /// <summary>The type of the push notification.</summary>
    public string? Classification { get; }
}