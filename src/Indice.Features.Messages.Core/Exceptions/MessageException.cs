using Indice.Types;

namespace Indice.Features.Messages.Core.Exceptions;

/// <summary>Exception thrown from Campaigns API feature.</summary>
public class MessageExceptions
{

    /// <summary>Campaign already published exception.</summary>
    /// <param name="id">The campaign id.</param>
    public static BusinessException CampaignAlreadyPublished(Guid id) => new($"Campaign with id '{id}' is already published.", nameof(CampaignAlreadyPublished));
    /// <summary>Campaign not found exception.</summary>
    /// <param name="id">The campaign id.</param>
    public static BusinessException CampaignNotFound(Guid id) => new($"Campaign with id '{id}' does not exist.", nameof(CampaignNotFound));
    /// <summary>Contact not found exception.</summary>
    /// <param name="id">The contact id.</param>
    public static BusinessException ContactNotFound(Guid id) => new($"Contact with id '{id}' does not exist.", nameof(ContactNotFound));
    /// <summary>Distribution list not found exception.</summary>
    /// <param name="id">The distribution list id.</param>
    public static BusinessException DistributionListNotFound(Guid id) => new($"Distribution list with id '{id}' does not exist.", nameof(DistributionListNotFound));
    /// <summary>Message type not found exception.</summary>
    /// <param name="id">The message type id.</param>
    public static BusinessException MessageTypeNotFound(Guid id) => new($"Message type with id '{id}' does not exist.", nameof(MessageTypeNotFound));
    /// <summary>Message not found exception.</summary>
    /// <param name="id">The message id.</param>
    public static BusinessException MessageNotFound(Guid id) => new($"Message with id '{id}' does not exist.", nameof(MessageNotFound));
    /// <summary>Message already read exception.</summary>
    /// <param name="id">The message id.</param>
    public static BusinessException MessageAlreadyRead(Guid id) => new($"Message with id '{id}' is already read.", nameof(MessageAlreadyRead));
    /// <summary>Message already deleted exception.</summary>
    /// <param name="id">The message id.</param>
    public static BusinessException MessageAlreadyDeleted(Guid id) => new($"Message with id '{id}' is already deleted.", nameof(MessageAlreadyDeleted));
    /// <summary>Distribution list not associated with contact exception.</summary>
    /// <param name="id">The id of the distribution list.</param>
    /// <param name="contactId">The id of the contact.</param>
    public static BusinessException DistributionListContactAssociationNotFound(Guid id, Guid contactId) => new($"Distribution list with id '{id}' does not contain contact with id '{contactId}'.", nameof(DistributionListContactAssociationNotFound));
    /// <summary>Contact already in distribution list exception.</summary>
    /// <param name="id">The id of the distribution list.</param>
    /// <param name="contactId">The id of the contact.</param>
    public static BusinessException ContactAlreadyInDistributionList(Guid id, Guid contactId) => new($"Contact with id '{contactId}' already belongs to distribution list with id '{id}'.", nameof(ContactAlreadyInDistributionList));
    /// <summary>Distribution list associated with one or more campaigns exception.</summary>
    /// <param name="name">The name of the distribution list.</param>
    /// <param name="campaignsCount">The number of associated campaigns.</param>
    public static BusinessException DistributionListAssociatedWithCampaigns(string name, int campaignsCount) =>
        new($"Distribution list '{name}' is associated with {campaignsCount} campaign(s). Please remove any association, if possible, and try again.", nameof(DistributionListAssociatedWithCampaigns));
    /// <summary>Template not found exception.</summary>
    /// <param name="id">The template id.</param>
    public static BusinessException TemplateNotFound(Guid id) => new($"Template with id '{id}' does not exist.", nameof(TemplateNotFound));
    /// <summary>Attachment not found exception.</summary>
    /// <param name="attachmentId">The attachment id.</param>
    public static BusinessException AttachmentNotFound(Guid attachmentId) => new($"Campaign with id '{attachmentId}' does not exist.", nameof(CampaignNotFound));
    /// <summary>Message sender not found exception.</summary>                                                                                                                                           
    /// <param name="id">The message sender id.</param>
    public static BusinessException MessageSenderNotFound(Guid id) => new($"Message sender with id '{id}' does not exist.", nameof(MessageSenderNotFound));
}
