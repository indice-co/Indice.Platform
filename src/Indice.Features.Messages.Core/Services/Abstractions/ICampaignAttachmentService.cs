using Indice.Features.Messages.Core.Models;

namespace Indice.Features.Messages.Core.Services.Abstractions;

/// <summary>A service that contains campaign attachments related operations.</summary>
public interface ICampaignAttachmentService
{
    /// <summary>Creates a new attachment.</summary>
    /// <param name="fileAttachment">The file attachment.</param>
    Task<AttachmentLink> Create(FileAttachment fileAttachment);
    /// <summary>Associates a campaign with an attachment.</summary>
    /// <param name="campaignId">The id of the campaign.</param>
    /// <param name="attachmentId">The id of the attachment.</param>
    Task Associate(Guid campaignId, Guid attachmentId);
    /// <summary>Deletes a campaign attachment.</summary>
    /// <param name="campaignId">The id of the campaign.</param>
    /// <param name="attachmentId">The id of the attachment.</param>
    Task Delete(Guid campaignId, Guid attachmentId);
    /// <summary>Retrieves an attachment associated with a campaign.</summary>
    /// <param name="campaignId"></param>
    /// <param name="attachmentId"></param>
    /// <returns></returns>
    Task<FileAttachment> GetFile(Guid campaignId, Guid attachmentId);
}
