using Indice.AspNetCore.Features.Campaigns.Models;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    public interface IMessageService
    {
        Task Create(CreateMessageRequest request);
    }
}
