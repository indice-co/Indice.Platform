using System;
using System.Threading.Tasks;

namespace Indice.Services;

/// <summary>Exception for SMS service failure.</summary>
public class SmsServiceException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="SmsServiceException"/> class.</summary>
    public SmsServiceException() { }

    /// <summary>Initializes a new instance of the <see cref="SmsServiceException"/> class with a specified error message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public SmsServiceException(string message) : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="SmsServiceException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.</summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public SmsServiceException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>Settings class for configuring SMS service clients.</summary>
public class SmsServiceSettings
{
    /// <summary>Key in the configuration.</summary>
    public static readonly string Name = "Sms";
    /// <summary>The API key.</summary>
    public string ApiKey { get; set; }
    /// <summary>The default sender.</summary>
    public string Sender { get; set; }
    /// <summary>The sender display name.</summary>
    public string SenderName { get; set; }
    /// <summary>If true then test mode should not charge any credits.</summary>
    public bool TestMode { get; set; }
    /// <summary>In case of viber failure fall-back to sms</summary>
    public bool ViberFallbackEnabled { get; set; } = false;
    /// <summary>The number of a seconds that a message is considered active. Defaults to 4320 seconds.</summary>
    public int Validity { get; set; } = 4320;
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
    Task SendAsync(string destination, string subject, string body);
}
