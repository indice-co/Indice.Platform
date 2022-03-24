using System;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Types;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    internal interface IInboxService
    {
        Task<UserMessage> GetMessageById(Guid messageId, string userCode);
        Task<ResultSet<UserMessage>> GetUserMessages(string userCode, ListOptions<UserMessageFilter> options);
        Task MarkMessageAsRead(Guid messageId, string userCode);
        Task MarkMessageAsDeleted(Guid messageId, string userCode);
    }
}
