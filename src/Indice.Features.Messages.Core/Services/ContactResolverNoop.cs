using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Types;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    /// <summary>
    /// An implementation of <see cref="IContactService"/> that does nothing.
    /// </summary>
    public class ContactResolverNoop : IContactResolver
    {
        /// <inheritdoc />
        public Task<ResultSet<Contact>> Find(ListOptions<ContactSearchFilter> options) => Task.FromResult(new ResultSet<Contact>());

        /// <inheritdoc />
        public Task<Contact> GetById(string id) => Task.FromResult<Contact>(null);
    }
}
