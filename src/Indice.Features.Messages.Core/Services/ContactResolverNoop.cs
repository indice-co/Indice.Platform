using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;

namespace Indice.Features.Messages.Core.Services;

/// <summary>An implementation of <see cref="IContactService"/> that does nothing.</summary>
public class ContactResolverNoop : IContactResolver
{
    /// <inheritdoc />
    public Task<ResultSet<ContactPreferences>> Find(ListOptions options) => Task.FromResult(new ResultSet<ContactPreferences>());

    /// <inheritdoc />
    public Task<ContactPreferences?> Resolve(string? recipientId) => Task.FromResult<ContactPreferences?>(null);
}
