using Indice.Features.Messages.Core.Models;
using Indice.Types;

namespace Indice.Features.Messages.Core.Services.Abstractions;

/// <summary>Contains information that help gather contact information from other systems.</summary>
public interface IContactResolver
{
    /// <summary>Specifies a way to resolve a contact from an external system.</summary>
    /// <param name="recipientId">The unique id of the contact.</param>
    Task<Contact> Resolve(string recipientId);
    /// <summary>Searches a list of contacts, using the specified criteria, from an external system.</summary>
    /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
    Task<ResultSet<Contact>> Find(ListOptions options);
}

/// <summary>Extensions on the <see cref="IContactResolver"/>.</summary>
public static class IContactResolverExtensions
{
    /// <summary>Resolves the contact and patches the given instance.</summary>
    /// <param name="resolver">The resolver.</param>
    /// <param name="recipientId">The unique id of the contact to resolve.</param>
    /// <param name="contact">The instance to patch.</param>
    public async static Task<Contact> Patch(this IContactResolver resolver, string recipientId, Contact contact) {
        var resolvedContact = await resolver.Resolve(recipientId);
        if (resolvedContact is not null) {
            resolvedContact.Id = contact.Id;
        }
        return resolvedContact;
    }
}
