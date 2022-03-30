using System;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Types;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    internal interface IInboxService
    {
        Task<Message> GetById(Guid id, string userCode);
        Task<ResultSet<Message>> GetList(string userCode, ListOptions<MessagesFilter> options);
        Task MarkAsRead(Guid id, string userCode);
        Task MarkAsDeleted(Guid id, string userCode);
    }
}
