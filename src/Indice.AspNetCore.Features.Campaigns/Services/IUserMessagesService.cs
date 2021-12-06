using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Types;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    internal interface IUserMessagesService
    {
        Task<UserMessage> GetMessageById(Guid messageId, string userCode);
        Task<int> GetNumberOfUnreadMessages(string userCode);
        Task<ResultSet<UserMessage, IEnumerable<CampaignType>>> GetUserMessages(string userCode, ListOptions<GetMessagesListFilter> options);
        Task MarkMessageAsRead(Guid messageId, string userCode);
        Task MarkMessageAsDeleted(Guid messageId, string userCode);
    }
}
