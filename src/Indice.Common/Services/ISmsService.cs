using System.Diagnostics;
using System.Web;

namespace Indice.Services;

/// <summary>The representation of a sender id visible in the recipients phone. i.e. INDICE. Defaults to the configuration values <strong>Sms:Sender</strong> and <strong>Sms:SenderName</strong>.</summary>
public class SmsSender
{
    /// <summary>Creates a new instance of <see cref="SmsSender"/>.</summary>
    /// <param name="senderId">Sender id.</param>
    /// <param name="displayName">Display name.</param>
    public SmsSender(string senderId, string displayName) {
        Id = senderId;
        DisplayName = displayName;
    }

    /// <summary>Sender id.</summary>
    public string Id { get; }
    /// <summary>Sender Name.</summary>
    public string DisplayName { get; }
    /// <summary>Checks for id existence.</summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(Id);
    /// <inheritdoc/>
    public override string ToString() => IsEmpty ? base.ToString()! : $"{DisplayName} <{Id}>";
}

/// <summary>Exception for SMS service failure.</summary>
public class SmsServiceException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="SmsServiceException"/> class.</summary>
    public SmsServiceException() { }

    /// <summary>Initializes a new instance of the <see cref="SmsServiceException"/> class with a specified error message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public SmsServiceException(string? message) : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="SmsServiceException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.</summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public SmsServiceException(string? message, Exception? innerException) : base(message, innerException) { }
}

/// <summary>Settings class for configuring SMS service clients.</summary>
public class SmsServiceSettings
{
    /// <summary>Key in the configuration. <strong>Sms</strong></summary>
    public static readonly string Name = "Sms";
    /// <summary>The API key.</summary>
    public string? ApiKey { get; set; }
    /// <summary>The default sender.</summary>
    public string? Sender { get; set; }
    /// <summary>The sender display name.</summary>
    public string? SenderName { get; set; }
    /// <summary>If true then test mode should not charge any credits.</summary>
    public bool TestMode { get; set; }
    /// <summary>In case of Viber failure fall-back to SMS.</summary>
    public bool ViberFallbackEnabled { get; set; } = false;
    /// <summary>The number of a seconds that a message is considered active. Defaults to 4320 seconds.</summary>
    public int Validity { get; set; } = 4320;
}



/// <summary>Webhook strategy for building report URLs.</summary>
public enum WebHookStrategy
{
    /// <summary>Use the BaseUrl as-is without modifications.</summary>
    Static,
    /// <summary>Append custom query string parameters to the BaseUrl.</summary>
    QueryString,
    /// <summary>Use a custom URL builder function to generate the URL.</summary>
    Custom
}

/// <summary>Settings for configuring webhook delivery reports.</summary>
public class WebHookSettings<T> where T : 
{
    /// <summary>The base URL for the webhook endpoint.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>The strategy to use when building the report URL.</summary>
    public WebHookStrategy Strategy { get; set; } = WebHookStrategy.Static;

    /// <summary>The signature method used for webhook security.</summary>
    public string? SignatureMethod { get; set; }

    /// <summary>Custom query string parameters to append when using QueryString strategy.</summary>
    public Dictionary<string, string>? QueryStringParameters { get; set; }

    /// <summary>Custom URL builder function used when Strategy is set to Custom.</summary>
    /// <remarks>
    /// This function receives the BaseUrl and should return the fully constructed URL.
    /// Only used when Strategy is set to <see cref="WebHookStrategy.Custom"/>.
    /// </remarks>
    public Func<string, string>? CustomUrlBuilder { get; set; }

    /// <summary>QueryString builder function used when Strategy is set to QueryString.</summary>
    /// <remarks>
    /// This function receives the BaseUrl and should return the fully constructed URL.
    /// Only used when Strategy is set to <see cref="WebHookStrategy.QueryString"/>.
    /// </remarks>
    public Func<string, string>? QueryStringUrlBuilder { get; set; }

    /// <summary>Builds the report URL based on the configured strategy.</summary>
    /// <returns>The constructed report URL, or null if BaseUrl is not set.</returns>
    /// <exception cref="InvalidOperationException">Thrown when Strategy is Custom but UrlBuilder is not set.</exception>
    public string? BuildUrl() {
        if (string.IsNullOrWhiteSpace(BaseUrl)) {
            return null;
        }

        return Strategy switch {
            WebHookStrategy.Static => BaseUrl,
            WebHookStrategy.QueryString => BuildUrlWithQueryString(),
            WebHookStrategy.Custom => BuildUrlWithCustomBuilder(),
            _ => BaseUrl
        };
    }

    private string BuildUrlWithQueryString() {
        if (QueryStringUrlBuilder == null) {
            throw new InvalidOperationException(
                "UrlBuilder must be set when using Custom strategy. " +
                "Please provide a custom URL builder function or use a different strategy.");
        }

        return QueryStringUrlBuilder(BaseUrl!);
    }

    private string BuildUrlWithCustomBuilder() {
        if (QueryStringUrlBuilder == null) {
            throw new InvalidOperationException(
                "UrlBuilder must be set when using Custom strategy. " +
                "Please provide a custom URL builder function or use a different strategy.");
        }

        return QueryStringUrlBuilder(BaseUrl!);
    }
}

/// <summary> The send receipt object representing a send message by either the <see cref="IEmailService"/> or the <see cref="ISmsService"/>. </summary>
[DebuggerDisplay("{ToString(),nq} ({InsertionTime,nq})")]
public record SendReceipt
{
    /// <summary> Initializes a new instance of <see cref="SendReceipt"/>. </summary>
    /// <param name="messageId"> The Id of the Message. </param>
    /// <param name="insertionTime"> The time the Message was inserted into the Queue. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="messageId"/></exception>
    public SendReceipt(string messageId, DateTimeOffset insertionTime) {
        
        MessageId = messageId ?? throw new ArgumentNullException(nameof(messageId));
        InsertionTime = insertionTime == default ? DateTimeOffset.UtcNow : insertionTime;
    }

    /// <summary>The id of the message</summary>
    public string MessageId { get; }
    /// <summary>The time it was dispatched</summary>
    public DateTimeOffset InsertionTime { get; }

    /// <inheritdoc/>
    public override string ToString() {
        return $"msg: {MessageId ?? "(empty)"}";
    }
}


/// <summary>SMS service abstraction in order support different providers.</summary>
public interface ISmsService
{
    /// <summary>Checks the implementation if supports the given <paramref name="deliveryChannel"/>.</summary>
    /// <param name="deliveryChannel">A string representing the delivery channel. i.e 'SMS'</param>
    /// <returns></returns>
    bool Supports(string deliveryChannel);
    /// <summary>Sends the SMS using the configured provider.</summary>
    /// <param name="destination">Destination, i.e. the phone number</param>
    /// <param name="subject">Message subject.</param>
    /// <param name="body">Message content.</param>
    /// <param name="sender">The sender id visible in the recipients phone. i.e. INDICE. Defaults to the configuration value <strong>sender</strong>.</param>
    Task<SendReceipt> SendAsync(string destination, string subject, string? body, SmsSender? sender = null);
}
