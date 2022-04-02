using Indice.AspNetCore.Features.Campaigns.Models;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    /// <summary>
    /// An implementation of <see cref="IContactService"/> that does nothing.
    /// </summary>
    public class ContactResolverNoop : IContactResolver
    {
        /// <inheritdoc />
        public Task<Contact> Resolve(string id) => Task.FromResult<Contact>(null);
    }
}
