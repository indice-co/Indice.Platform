using System;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Types;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    internal interface IInboxService
    {
        Task<Message> GetMessageById(Guid messageId, string userCode);
        Task<ResultSet<Message>> GetMessages(string userCode, ListOptions<MessagesFilter> options);
        Task MarkMessageAsRead(Guid messageId, string userCode);
        Task MarkMessageAsDeleted(Guid messageId, string userCode);
    }
}
