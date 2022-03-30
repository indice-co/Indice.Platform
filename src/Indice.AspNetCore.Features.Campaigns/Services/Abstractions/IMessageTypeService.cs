using System;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Types;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    internal interface IMessageTypeService
    {
        Task<ResultSet<MessageType>> GetList(ListOptions options);
        Task<MessageType> GetById(Guid id);
        Task<MessageType> GetByName(string name);
        Task<MessageType> Create(UpsertMessageTypeRequest request);
        Task Update(Guid id, UpsertMessageTypeRequest request);
        Task Delete(Guid id);
    }
}
