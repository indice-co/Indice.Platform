using Indice.AspNetCore.Features.Campaigns.Models;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    public class ContactResolverNoop : IContactResolver
    {
        public Task<Contact> Resolve(string id) => Task.FromResult<Contact>(null);
    }
}
