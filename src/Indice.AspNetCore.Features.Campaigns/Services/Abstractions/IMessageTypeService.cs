using System;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Types;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    internal interface IMessageTypeService
    {
        Task<ResultSet<MessageType>> GetList(ListOptions options);
        Task<MessageType> GetById(Guid campaignTypeId);
        Task<MessageType> GetByName(string name);
        Task<MessageType> Create(UpsertMessageTypeRequest request);
        Task<bool> Update(Guid campaignTypeId, UpsertMessageTypeRequest request);
        Task<bool> Delete(Guid campaignTypeId);
    }
}
