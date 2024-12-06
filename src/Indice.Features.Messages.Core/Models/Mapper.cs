using System.Dynamic;
using System.Linq.Expressions;
using Indice.Extensions;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Manager.Commands;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Types;

namespace Indice.Features.Messages.Core.Models;

internal static class Mapper
{
    public static Expression<Func<DbCampaign, Campaign>> ProjectToCampaign = campaign => new() {
        ActionLink = campaign.ActionLink,
        MediaBaseHref = campaign.MediaBaseHref,
        ActivePeriod = campaign.ActivePeriod,
        Content = campaign.Content ?? new(),
        CreatedAt = campaign.CreatedAt,
        CreatedBy = campaign.CreatedBy,
        UpdatedAt = campaign.UpdatedAt,
        UpdatedBy = campaign.UpdatedBy,
        Data = campaign.Data,
        MessageChannelKind = campaign.MessageChannelKind,
        DistributionList = campaign.DistributionList != null ? new DistributionList {
            CreatedAt = campaign.DistributionList.CreatedAt,
            CreatedBy = campaign.DistributionList.CreatedBy,
            UpdatedAt = campaign.DistributionList.UpdatedAt,
            UpdatedBy = campaign.DistributionList.UpdatedBy,
            Id = campaign.DistributionList.Id,
            Name = campaign.DistributionList.Name
        } : null,
        Id = campaign.Id,
        IsGlobal = campaign.IsGlobal,
        Published = campaign.Published,
        IgnoreUserPreferences = campaign.IgnoreUserPreferences,
        Title = campaign.Title,
        Type = campaign.Type != null ? new MessageType {
            Id = campaign.Type.Id,
            Name = campaign.Type.Name,
            Classification = campaign.Type.Classification
        } : null
    };

    public static Campaign ToCampaign(DbCampaign campaign) => ProjectToCampaign.Compile()(campaign);

    public static Expression<Func<DbContact, Contact>> ProjectToContact = contact => new() {
        Email = contact.Email,
        FirstName = contact.FirstName,
        FullName = contact.FullName,
        Id = contact.Id,
        LastName = contact.LastName,
        PhoneNumber = contact.PhoneNumber,
        CommunicationPreferences = contact.CommunicationPreferences,
        ConsentCommercial = contact.ConsentCommercial,
        Locale = contact.Locale,
        RecipientId = contact.RecipientId,
        Salutation = contact.Salutation,
        UpdatedAt = contact.UpdatedAt,
        Unsubscribed = contact.DistributionListContacts.Any() && contact.DistributionListContacts[0].Unsubscribed
    };

    public static Contact ToContact(DbContact contact) => ProjectToContact.Compile()(contact);

    public static CreateContactRequest ToCreateContactRequest(Contact request) => new() {
        Email = request.Email,
        FirstName = request.FirstName,
        FullName = request.FullName,
        LastName = request.LastName,
        PhoneNumber = request.PhoneNumber,
        RecipientId = request.RecipientId,
        Salutation = request.Salutation,
        CommunicationPreferences = request.CommunicationPreferences,
        ConsentCommercial = request.ConsentCommercial,
        Locale = request.Locale
    };

    public static UpdateContactRequest ToUpdateContactRequest(Contact request, Guid? distributionListId = null) => new() {
        DistributionListId = distributionListId,
        Email = request.Email,
        FirstName = request.FirstName,
        FullName = request.FullName,
        LastName = request.LastName,
        PhoneNumber = request.PhoneNumber,
        Salutation = request.Salutation,
        CommunicationPreferences = request.CommunicationPreferences,
        ConsentCommercial = request.ConsentCommercial,
        Locale = request.Locale
    };

    public static Expression<Func<DbCampaign, CampaignDetails>> ProjectToCampaignDetails = campaign => new() {
        ActionLink = campaign.ActionLink,
        MediaBaseHref = campaign.MediaBaseHref,
        ActivePeriod = campaign.ActivePeriod,
        Attachment = campaign.Attachment != null ? new AttachmentLink {
            Id = campaign.Attachment.Id,
            ContentType = campaign.Attachment.ContentType,
            Label = campaign.Attachment.Name,
            Size = campaign.Attachment.ContentLength,
            PermaLink = $"/campaigns/attachments/{(Base64Id)campaign.Attachment.Guid}.{Path.GetExtension(campaign.Attachment.Name).TrimStart('.')}"
        } : null,
        Content = campaign.Content ?? new(),
        CreatedAt = campaign.CreatedAt,
        CreatedBy = campaign.CreatedBy,
        UpdatedAt = campaign.UpdatedAt,
        UpdatedBy = campaign.UpdatedBy,
        Data = campaign.Data,
        MessageChannelKind = campaign.MessageChannelKind,
        DistributionList = campaign.DistributionList != null ? new DistributionList {
            CreatedAt = campaign.DistributionList.CreatedAt,
            CreatedBy = campaign.DistributionList.CreatedBy,
            Id = campaign.DistributionList.Id,
            Name = campaign.DistributionList.Name
        } : null,
        Id = campaign.Id,
        Published = campaign.Published,
        IgnoreUserPreferences = campaign.IgnoreUserPreferences,
        IsGlobal = campaign.IsGlobal,
        Title = campaign.Title,
        Type = campaign.Type != null ? new MessageType {
            Id = campaign.Type.Id,
            Name = campaign.Type.Name,
            Classification = campaign.Type.Classification
        } : null
    };

    public static CampaignDetails ToCampaignDetails(DbCampaign campaign) => ProjectToCampaignDetails.Compile()(campaign);

    public static DbCampaign ToDbCampaign(CreateCampaignRequest request) => new() {
        ActionLink = request.ActionLink,
        MediaBaseHref = request.MediaBaseHref,
        ActivePeriod = request.ActivePeriod,
        Content = request.Content,
        CreatedAt = DateTime.UtcNow,
        Data = request.Data,
        DistributionListId = request.RecipientListId,
        Id = Guid.NewGuid(),
        IsGlobal = request.IsGlobal,
        MessageChannelKind = Enum.Parse<MessageChannelKind>(string.Join(',', request.Content.Select(x => x.Key)), ignoreCase: true),
        Published = request.Published,
        IgnoreUserPreferences = request.IgnoreUserPreferences ?? false,
        Title = request.Title,
        TypeId = request.TypeId,
        AttachmentId = request.AttachmentIds?.Cast<Guid?>().FirstOrDefault()
    };

    public static DbContact ToDbContact(CreateDistributionListContactRequest request) => new() {
        Email = request.Email,
        FirstName = request.FirstName,
        FullName = request.FullName,
        Id = request.ContactId ?? Guid.NewGuid(),
        LastName = request.LastName,
        PhoneNumber = request.PhoneNumber,
        RecipientId = request.RecipientId,
        Salutation = request.Salutation,
        CommunicationPreferences = request.CommunicationPreferences,
        ConsentCommercial = request.ConsentCommercial,
        Locale = request.Locale,
        UpdatedAt = DateTimeOffset.UtcNow
    };
    public static DbContact ToDbContact(Contact contact) => new() {
        Email = contact.Email,
        FirstName = contact.FirstName,
        FullName = contact.FullName,
        Id = contact.Id ?? Guid.NewGuid(),
        LastName = contact.LastName,
        PhoneNumber = contact.PhoneNumber,
        RecipientId = contact.RecipientId,
        Salutation = contact.Salutation,
        CommunicationPreferences = contact.CommunicationPreferences,
        ConsentCommercial = contact.ConsentCommercial,
        Locale = contact.Locale,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    public static CreateDistributionListContactRequest ToCreateDistributionListContactRequest(Contact contact) => new() {
        Email = contact.Email,
        FirstName = contact.FirstName,
        FullName = contact.FullName,
        ContactId = contact.Id,
        LastName = contact.LastName,
        PhoneNumber = contact.PhoneNumber,
        RecipientId = contact.RecipientId,
        CommunicationPreferences = contact.CommunicationPreferences,
        ConsentCommercial = contact.ConsentCommercial,
        Locale = contact.Locale,
        Salutation = contact.Salutation
    };

    public static DbContact ToDbContact(CreateContactRequest request) => new() {
        Email = request.Email,
        FirstName = request.FirstName,
        FullName = request.FullName,
        Id = Guid.NewGuid(),
        LastName = request.LastName,
        PhoneNumber = request.PhoneNumber,
        CommunicationPreferences = request.CommunicationPreferences,
        Locale = request.Locale,
        ConsentCommercial = request.ConsentCommercial,
        RecipientId = request.RecipientId,
        Salutation = request.Salutation,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    public static void MapFromCreateDistributionListContactRequest(this DbContact contact, CreateDistributionListContactRequest request) {
        contact.Email = request.Email;
        contact.FirstName = request.FirstName;
        contact.FullName = request.FullName;
        contact.LastName = request.LastName;
        contact.PhoneNumber = request.PhoneNumber;
        contact.RecipientId = request.RecipientId;
        contact.Salutation = request.Salutation;
        contact.CommunicationPreferences = request.CommunicationPreferences;
        contact.Locale = request.Locale;
        contact.ConsentCommercial = request.ConsentCommercial;
        contact.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static DbAttachment ToDbAttachment(FileAttachment fileAttachment) => new() {
        ContentLength = fileAttachment.ContentLength,
        ContentType = fileAttachment.ContentType,
        Data = fileAttachment.Data,
        FileExtension = fileAttachment.FileExtension,
        Guid = fileAttachment.Guid,
        Id = fileAttachment.Id,
        Name = fileAttachment.Name,
        Uri = fileAttachment.Uri
    };

    public static CreateCampaignCommand ToCreateCampaignCommand(CreateCampaignRequest request) => new() {
        ActionLink = request.ActionLink,
        ActivePeriod = request.ActivePeriod,
        Content = request.Content.ToDictionary(x => Enum.Parse<MessageChannelKind>(x.Key, ignoreCase: true), y => y.Value),
        Data = request.Data,
        DistributionListId = request.RecipientListId,
        IsGlobal = request.IsGlobal,
        Published = request.Published,
        RecipientIds = request.RecipientIds,
        Recipients = request.Recipients,
        Title = request.Title,
        Type = request.TypeId.HasValue ? new MessageType { Id = request.TypeId.Value } : null
    };

    public static CreateCampaignRequest ToCreateCampaignRequest(CreateCampaignCommand command) => new() {
        ActionLink = command.ActionLink,
        ActivePeriod = command.ActivePeriod,
        Content = new MessageContentDictionary(command.Content),
        Data = ToExpandoObject(command.Data),
        RecipientListId = command.DistributionListId,
        IsGlobal = command.IsGlobal,
        Published = command.Published,
        RecipientIds = command.RecipientIds,
        Recipients = command.Recipients,
        Title = command.Title,
        TypeId = command.Type?.Id
    };

    public static ExpandoObject ToExpandoObject(object value) => value.ToExpandoObject();
}
