using System;
using System.Threading.Tasks;

namespace Indice.Services
{
    /// <summary>
    /// Exception for sms service failure
    /// </summary>
    public class SmsServiceException : Exception
    {
        /// <summary>
        /// constructs an <see cref="SmsServiceException"/>
        /// </summary>
        /// <param name="message"></param>
        public SmsServiceException(string message) : base(message) {
        }
    }

    /// <summary>
    /// Settings class for configuring Sms serice clients
    /// </summary>
    public class SmsServiceSettings
    {
        /// <summary>
        /// Key in the configureation
        /// </summary>
        public static readonly string Name = "Sms";

        /// <summary>
        /// The api key 
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// The default sender
        /// </summary>
        public string Sender { get; set; }

        /// <summary>
        /// The sender display name
        /// </summary>
        public string SenderName { get; set; }

        /// <summary>
        /// If true then test mode should not charge any credits.
        /// </summary>
        public bool TestMode { get; set; }
    }

    /// <summary>
    /// Sms service abstraction in order support different providers.
    /// </summary>
    public interface ISmsService
    {
        /// <summary>
        /// SMS send service.
        /// </summary>
        /// <param name="destination">Destination, i.e. To phone number</param>
        /// <param name="subject">Subject</param>
        /// <param name="body">Message contents</param>
        /// <returns></returns>
        Task SendAsync(string destination, string subject, string body);
    }
}
