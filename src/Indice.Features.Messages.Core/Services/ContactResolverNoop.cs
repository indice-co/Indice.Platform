using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;

namespace Indice.Features.Messages.Core.Services
{
    /// <summary>
    /// An implementation of <see cref="IContactService"/> that does nothing.
    /// </summary>
    public class ContactResolverNoop : IContactResolver
    {
        /// <inheritdoc />
        public Task<ResultSet<Contact>> Find(ListOptions options) => Task.FromResult(new ResultSet<Contact>());

        /// <inheritdoc />
        public Task<Contact> Resolve(string id) => Task.FromResult<Contact>(null);
    }
}
