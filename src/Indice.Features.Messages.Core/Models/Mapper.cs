using System.Linq.Expressions;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Indice.Types;

namespace Indice.AspNetCore.Features.Campaigns.Models
{
    internal class Mapper
    {
        public static Expression<Func<DbCampaign, Campaign>> ProjectToCampaign = campaign => new() {
            ActionLink = campaign.ActionLink,
            ActivePeriod = campaign.ActivePeriod,
            Content = campaign.Content,
            CreatedAt = campaign.CreatedAt,
            Data = campaign.Data,
            DeliveryChannel = campaign.DeliveryChannel,
            DistributionList = campaign.DistributionList != null ? new DistributionList {
                Id = campaign.DistributionList.Id,
                Name = campaign.DistributionList.Name
            } : null,
            Id = campaign.Id,
            IsGlobal = campaign.IsGlobal,
            Published = campaign.Published,
            Title = campaign.Title,
            Type = campaign.Type != null ? new MessageType {
                Id = campaign.Type.Id,
                Name = campaign.Type.Name
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
            RecipientId = contact.RecipientId,
            Salutation = contact.Salutation,
            UpdatedAt = contact.UpdatedAt
        };

        public static Contact ToContact(DbContact contact) => ProjectToContact.Compile()(contact);

        public static CreateContactRequest ToCreateContactRequest(Contact request) => new() {
            Email = request.Email,
            FirstName = request.FirstName,
            FullName = request.FullName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            RecipientId = request.RecipientId,
            Salutation = request.Salutation
        };

        public static UpdateContactRequest ToUpdateContactRequest(Contact request) => new() {
            Email = request.Email,
            FirstName = request.FirstName,
            FullName = request.FullName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            Salutation = request.Salutation
        };

        public static Expression<Func<DbCampaign, CampaignDetails>> ProjectToCampaignDetails = campaign => new() {
            ActionLink = campaign.ActionLink,
            ActivePeriod = campaign.ActivePeriod,
            Attachment = campaign.Attachment != null ? new AttachmentLink {
                Id = campaign.Attachment.Id,
                ContentType = campaign.Attachment.ContentType,
                Label = campaign.Attachment.Name,
                Size = campaign.Attachment.ContentLength,
                PermaLink = $"/campaigns/attachments/{(Base64Id)campaign.Attachment.Guid}.{Path.GetExtension(campaign.Attachment.Name).TrimStart('.')}"
            } : null,
            Content = campaign.Content,
            CreatedAt = campaign.CreatedAt,
            Data = campaign.Data,
            DeliveryChannel = campaign.DeliveryChannel,
            DistributionList = campaign.DistributionList != null ? new DistributionList {
                Id = campaign.DistributionList.Id,
                Name = campaign.DistributionList.Name
            } : null,
            Id = campaign.Id,
            Published = campaign.Published,
            IsGlobal = campaign.IsGlobal,
            Title = campaign.Title,
            Type = campaign.Type != null ? new MessageType {
                Id = campaign.Type.Id,
                Name = campaign.Type.Name
            } : null
        };

        public static CampaignDetails ToCampaignDetails(DbCampaign campaign) => ProjectToCampaignDetails.Compile()(campaign);

        public static DbCampaign ToDbCampaign(CreateCampaignRequest request) => new() {
            ActionLink = request.ActionLink,
            Content = request.Content,
            CreatedAt = DateTime.UtcNow,
            Data = request.Data,
            DeliveryChannel = request.DeliveryChannel,
            DistributionListId = request.DistributionListId,
            Id = Guid.NewGuid(),
            IsGlobal = request.IsGlobal,
            Published = request.Published,
            Title = request.Title,
            TypeId = request.TypeId
        };

        public static DbContact ToDbContact(CreateDistributionListContactRequest request) => new() {
            Email = request.Email,
            FirstName = request.FirstName,
            FullName = request.FullName,
            Id = request.Id ?? Guid.NewGuid(),
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            RecipientId = request.RecipientId,
            Salutation = request.Salutation,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        public static DbContact ToDbContact(CreateContactRequest request) => new() {
            Email = request.Email,
            FirstName = request.FirstName,
            FullName = request.FullName,
            Id = Guid.NewGuid(),
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            RecipientId = request.RecipientId,
            Salutation = request.Salutation,
            UpdatedAt = DateTimeOffset.UtcNow
        };

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
    }
}
