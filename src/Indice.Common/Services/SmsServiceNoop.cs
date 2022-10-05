using System.Threading.Tasks;

namespace Indice.Services
{
    /// <summary>A default implementation for <see cref="ISmsService"/> that does nothing.</summary>
    public class SmsServiceNoop : ISmsService
    {
        /// <inheritdoc />
        public Task SendAsync(string destination, string subject, string body) => Task.CompletedTask;

        /// <inheritdoc />
        public bool Supports(string deliveryChannel) => false;
    }
}
