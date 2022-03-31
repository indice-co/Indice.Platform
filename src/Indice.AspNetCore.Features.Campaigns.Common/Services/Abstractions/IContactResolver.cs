using Indice.AspNetCore.Features.Campaigns.Models;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    /// <summary>
    /// Contains information that help gather contact information from other systems.
    /// </summary>
    public interface IContactResolver
    {
        /// <summary>
        /// Specifies a way to resolve a contact from an external system.
        /// </summary>
        /// <param name="id">The unique id of the contact.</param>
        Task<Contact> Resolve(string id);
    }
}
