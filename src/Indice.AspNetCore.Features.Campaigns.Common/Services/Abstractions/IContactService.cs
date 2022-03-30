using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Types;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    public interface IContactService
    {
        Task<ResultSet<Contact>> GetList(ListOptions options);
        Task<Contact> GetById(Guid id);
        Task AddToDistributionList(CreateDistributionListContactRequest request, Guid id);
        Task<Contact> Create(CreateContactRequest request);
    }
}
