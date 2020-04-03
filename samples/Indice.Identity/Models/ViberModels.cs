using System.Text.Json.Serialization;

namespace Indice.Identity.Models
{
    public abstract class ViberEvent
    {
        /// <summary>
        /// Callback type - which event triggered the callback.
        /// </summary>
        public string Event { get; set; }
        /// <summary>
        /// Time (epoch) of the event that triggered the callback.
        /// </summary>
        public string Timestamp { get; set; }
        /// <summary>
        /// Unique ID of the message.
        /// </summary>
        [JsonPropertyName("message_token")]
        public string MessageToken { get; set; }
    }

    // https://developers.viber.com/docs/api/rest-bot-api/#subscribed
    public class SubscribedEvent : ViberEvent
    {
        /// <summary>
        /// User info.
        /// </summary>
        public ViberUser User { get; set; }
    }

    // https://developers.viber.com/docs/api/rest-bot-api/#unsubscribed
    public class UnsubscribedEvent : ViberEvent
    {
        /// <summary>
        /// Unique Viber user id.
        /// </summary>
        [JsonPropertyName("user_id")]
        public string UserId { get; set; }
    }

    // https://developers.viber.com/docs/api/rest-bot-api/#conversation-started
    public class ConversationStartedEvent : ViberEvent
    {
        /// <summary>
        /// The specific type of conversation_started event.
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// Any additional parameters added to the deep link used to access the conversation passed as a string.
        /// </summary>
        public string Context { get; set; }
        /// <summary>
        /// User info.
        /// </summary>
        public ViberUser User { get; set; }
        /// <summary>
        /// Indicated whether a user is already subscribed.
        /// </summary>
        public bool Subscribed { get; set; }
    }

    // https://developers.viber.com/docs/api/rest-bot-api/#delivered
    public class DeliveredEvent : ViberEvent
    {
        /// <summary>
        /// Unique Viber user id.
        /// </summary>
        [JsonPropertyName("user_id")]
        public string UserId { get; set; }
    }

    // https://developers.viber.com/docs/api/rest-bot-api/#seen
    public class SeenEvent : ViberEvent
    {
        /// <summary>
        /// Unique Viber user id.
        /// </summary>
        [JsonPropertyName("user_id")]
        public string UserId { get; set; }
    }

    // https://developers.viber.com/docs/api/rest-bot-api/#failed-callback
    public class FailedEvent : ViberEvent
    {
        /// <summary>
        /// Unique Viber user id.
        /// </summary>
        [JsonPropertyName("user_id")]
        public string UserId { get; set; }
        /// <summary>
        /// A string describing the failure.
        /// </summary>
        [JsonPropertyName("desc")]
        public string Description { get; set; }
    }

    // https://developers.viber.com/docs/api/rest-bot-api/#receive-message-from-user
    public class MessageEvent : ViberEvent
    {
        /// <summary>
        /// Message sender info.
        /// </summary>
        public ViberUser Sender { get; set; }
        /// <summary>
        /// The message content.
        /// </summary>
        public ViberMessage Message { get; set; }
    }

    public class ViberUser
    {
        /// <summary>
        /// Unique Viber user id.
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// User’s Viber name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// URL of user’s avatar.
        /// </summary>
        public string Avatar { get; set; }
        /// <summary>
        /// User’s 2 letter country code (ISO ALPHA-2 Code).
        /// </summary>
        public string Country { get; set; }
        /// <summary>
        /// User’s phone language. Will be returned according to the device language (ISO 639-1).
        /// </summary>
        public string Language { get; set; }
        /// <summary>
        /// The maximal Viber version that is supported by all of the user’s devices.
        /// </summary>
        [JsonPropertyName("api_version")]
        public int ApiVersion { get; set; }
    }

    public class ViberMessage
    {
        /// <summary>
        /// Message type (text, picture, video, file, sticker, contact, url and location).
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// The message text.
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// URL of the message media - can be image, video, file and url. Image/Video/File URLs will have a TTL of 1 hour.
        /// </summary>
        public string Media { get; set; }
        /// <summary>
        /// Location coordinates.
        /// </summary>
        public ViberLocation Location { get; set; }
        /// <summary>
        /// Contact details.
        /// </summary>
        public ViberContact Contact { get; set; }
        /// <summary>
        /// Tracking data sent with the last message to the user.
        /// </summary>
        [JsonPropertyName("tracking_data")]
        public string TrackingData { get; set; }
        /// <summary>
        /// File name.
        /// </summary>
        [JsonPropertyName("file_name")]
        public string FileName { get; set; }
        /// <summary>
        /// File size in bytes.
        /// </summary>
        [JsonPropertyName("file_size")]
        public int FileSize { get; set; }
        /// <summary>
        /// Video length in seconds.
        /// </summary>
        public int Duration { get; set; }
        /// <summary>
        /// Viber sticker id.
        /// </summary>
        [JsonPropertyName("sticker_id")]
        public string StickerId { get; set; }
    }

    public class ViberLocation
    {
        /// <summary>
        /// Latitude coordinate.
        /// </summary>
        [JsonPropertyName("lat")]
        public double Latitude { get; set; }
        /// <summary>
        /// Longitude coordinate.
        /// </summary>
        [JsonPropertyName("lon")]
        public double Longitude { get; set; }
    }

    public class ViberContact
    {
        /// <summary>
        /// Contact name.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Contact phone number.
        /// </summary>
        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; }
        /// <summary>
        /// Contact avatar.
        /// </summary>
        public string Avatar { get; set; }
    }
}
