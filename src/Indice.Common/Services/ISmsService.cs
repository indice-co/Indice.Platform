using System;
using System.Threading.Tasks;

namespace Indice.Services
{

    public class SmsServiceException : Exception
    {
        public SmsServiceException(string message) : base(message) {
        }
    }

    /// <summary>
    /// Settings class for configuring Sms serice clients
    /// </summary>
    public class SmsServiceSettings
    {

        public static readonly string Name = "Sms";
        public string ApiKey { get; set; }
        public string Sender { get; set; }
        public string SenderName { get; set; }
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
