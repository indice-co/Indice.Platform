using System;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Types;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    internal interface IMessagesService
    {
        Task<ResultSet<Message>> GetMessages(string userCode, ListOptions<GetMessagesListFilter> options);
        Task<Message> GetMessageById(Guid messageId, string userCode);
        Task MarkMessageAsRead(Guid messageId, string userCode);
        Task MarkMessageAsDeleted(Guid messageId, string userCode);
    }
}
