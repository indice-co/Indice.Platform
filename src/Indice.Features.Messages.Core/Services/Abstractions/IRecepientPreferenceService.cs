using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;

namespace Indice.Features.Messages.Core.Services.Abstractions;
/// <summary>A service that contains communication preference related operations.</summary>
public interface IRecepientPreferenceService
{
    /// <summary>Gets recipient preferences.</summary>
    /// <param name="recipientId">The id of the recipient.</param>
    Task<ContactPreference> GetPreferences(string recipientId);
    /// <summary>Updates an existing campaign.</summary>
    /// <param name="recipientId">The id of the recipient.</param>
    /// <param name="request">The data for the communication preferences.</param>
    Task Update(string recipientId, UpdatPreferenceRequest request);
    /// <summary>Updates an existing campaign.</summary>
    /// <param name="recipientId">The id of the recipient.</param>
    /// <param name="preference">The data for the communication preferences.</param>
    Task UpdateContactPreferences(string recipientId, ContactPreference preference);
}
