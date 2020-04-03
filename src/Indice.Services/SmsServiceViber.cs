using System;
using System.Threading.Tasks;

namespace Indice.Services
{
    /// <summary>
    /// Implementation of <see cref="ISmsService"/> using Viber's REST API.
    /// </summary>
    public class SmsServiceViber : ISmsService
    {
        /// <inheritdoc/>
        public Task SendAsync(string destination, string subject, string body) {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Settings class for configuring <see cref="SmsServiceViber"/>.
    /// </summary>
    public class SmsServiceViberSettings
    {
        /// <summary>
        /// Key in the configuration.
        /// </summary>
        public static readonly string Name = "Viber";
        /// <summary>
        /// The API key.
        /// </summary>
        public string ApiKey { get; set; }
    }
}
