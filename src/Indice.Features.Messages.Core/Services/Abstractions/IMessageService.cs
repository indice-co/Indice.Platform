using Indice.Features.Messages.Core.Models.Requests;

namespace Indice.Features.Messages.Core.Services.Abstractions;

/// <summary>A service that contains message related operations.</summary>
public interface IMessageService
{
    /// <summary>Creates a new inbox message.</summary>
    /// <param name="request">The data for the inbox message to create.</param>
    Task<Guid> Create(CreateMessageRequest request);
}
