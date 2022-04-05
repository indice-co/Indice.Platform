using Indice.AspNetCore.Features.Campaigns.Models;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    /// <summary>
    /// A service that contains message related operations.
    /// </summary>
    public interface IMessageService
    {
        /// <summary>
        /// Creates a new inbox message.
        /// </summary>
        /// <param name="request">The data for the inbox message to create.</param>
        Task Create(CreateMessageRequest request);
    }
}
