using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Controllers;
using Indice.AspNetCore.Features.Campaigns.Data;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Indice.AspNetCore.Features.Campaigns.Exceptions;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Configuration;
using Indice.Services;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    internal class CampaignService : ICampaignService
    {
        public CampaignService(
            CampaignsDbContext dbContext,
            IOptions<GeneralSettings> generalSettings,
            IOptions<CampaignManagementOptions> campaignManagementOptions,
            Func<string, IFileService> getFileService,
            IHttpContextAccessor httpContextAccessor,
            LinkGenerator linkGenerator // TODO: Decouple this service from LinkGenerator and IHttpContextAccessor
        ) {
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            CampaignManagementOptions = campaignManagementOptions?.Value ?? throw new ArgumentNullException(nameof(campaignManagementOptions));
            FileService = getFileService(KeyedServiceNames.FileServiceKey) ?? throw new ArgumentNullException(nameof(getFileService));
            GeneralSettings = generalSettings?.Value ?? throw new ArgumentNullException(nameof(generalSettings));
            HttpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            LinkGenerator = linkGenerator ?? throw new ArgumentNullException(nameof(linkGenerator));
        }

        public CampaignsDbContext DbContext { get; }
        public CampaignManagementOptions CampaignManagementOptions { get; }
        public IFileService FileService { get; }
        public GeneralSettings GeneralSettings { get; }
        public IHttpContextAccessor HttpContextAccessor { get; }
        public LinkGenerator LinkGenerator { get; }

        public Task<ResultSet<Campaign>> GetList(ListOptions<CampaignsFilter> options) {
            var query = DbContext
                .Campaigns
                .Include(x => x.Type)
                .Include(x => x.DistributionList)
                .AsNoTracking()
                .Select(Mapper.ProjectToCampaign);
            if (!string.IsNullOrEmpty(options.Search)) {
                var searchTerm = options.Search.Trim();
                query = query.Where(x => x.Title != null && x.Title.Contains(searchTerm));
            }
            if (options.Filter.DeliveryChannel.HasValue) {
                query = query.Where(x => x.DeliveryChannel.HasFlag(options.Filter.DeliveryChannel.Value));
            }
            if (options.Filter.Published.HasValue) {
                query = query.Where(x => x.Published == options.Filter.Published.Value);
            }
            return query.ToResultSetAsync(options);
        }

        public async Task<CampaignDetails> GetById(Guid id) {
            var campaign = await DbContext
                .Campaigns
                .AsNoTracking()
                .Include(x => x.Attachment)
                .Include(x => x.DistributionList)
                .Select(Mapper.ProjectToCampaignDetails)
                .SingleOrDefaultAsync(x => x.Id == id);
            if (campaign is null) {
                return default;
            }
            if (campaign.Attachment is not null) {
                campaign.Attachment.PermaLink = $"{CampaignManagementOptions.ApiPrefix}/{campaign.Attachment.PermaLink.TrimStart('/')}";
            }
            return campaign;
        }

        public async Task<Campaign> Create(CreateCampaignRequest request) {
            var dbCampaign = Mapper.ToDbCampaign(request);
            DbContext.Campaigns.Add(dbCampaign);
            await DbContext.SaveChangesAsync();
            // TODO: Take advantage of campaign created event and create message asynchronously. We possibly need to create messages when the campaign is published. Then the campaign should be locked.
            await CreateMessages(request, dbCampaign.Id);
            return Mapper.ToCampaign(dbCampaign);
        }

        public async Task Update(Guid id, UpdateCampaignRequest request) {
            var campaign = await DbContext.Campaigns.FindAsync(id);
            if (campaign is null) {
                throw CampaignException.CampaignNotFound(id);
            }
            campaign.ActionText = request.ActionText;
            campaign.ActivePeriod = request.ActivePeriod;
            campaign.Content = request.Content;
            campaign.Title = request.Title;
            await DbContext.SaveChangesAsync();
        }

        public async Task Delete(Guid id) {
            var campaign = await DbContext.Campaigns.FindAsync(id);
            if (campaign is null) {
                throw CampaignException.CampaignNotFound(id);
            }
            DbContext.Remove(campaign);
            await DbContext.SaveChangesAsync();
        }

        public async Task<AttachmentLink> CreateAttachment(IFormFile file) {
            var attachment = new DbAttachment();
            attachment.PopulateFrom(file);
            using (var stream = file.OpenReadStream()) {
                await FileService.SaveAsync($"campaigns/{attachment.Uri}", stream);
            }
            DbContext.Attachments.Add(attachment);
            await DbContext.SaveChangesAsync();
            return new AttachmentLink {
                ContentType = file.ContentType,
                FileGuid = attachment.Guid,
                Id = attachment.Id,
                Label = file.FileName,
                PermaLink = LinkGenerator.GetUriByAction(HttpContextAccessor.HttpContext, nameof(CampaignsController.GetCampaignAttachment), CampaignsController.Name, new {
                    fileGuid = (Base64Id)attachment.Guid,
                    format = Path.GetExtension(attachment.Name).TrimStart('.')
                }),
                Size = file.Length
            };
        }

        public async Task AssociateAttachment(Guid id, Guid attachmentId) {
            var campaign = await DbContext.Campaigns.FindAsync(id);
            if (campaign is null) {
                throw CampaignException.CampaignNotFound(id);
            }
            campaign.AttachmentId = attachmentId;
            await DbContext.SaveChangesAsync();
        }

        public async Task<CampaignStatistics> GetStatistics(Guid id) {
            var campaign = await DbContext.Campaigns.FindAsync(id);
            if (campaign is null) {
                return default;
            }
            var callToActionCount = await DbContext.Hits.AsNoTracking().CountAsync(x => x.CampaignId == id);
            var readCount = await DbContext.Messages.AsNoTracking().CountAsync(x => x.CampaignId == id && x.IsRead);
            var deletedCount = await DbContext.Messages.AsNoTracking().CountAsync(x => x.CampaignId == id && x.IsDeleted);
            int? notReadCount = null;
            if (!campaign.IsGlobal) {
                notReadCount = await DbContext.Messages.AsNoTracking().CountAsync(x => x.CampaignId == id && !x.IsRead);
            }
            return new CampaignStatistics {
                CallToActionCount = callToActionCount,
                DeletedCount = deletedCount,
                LastUpdated = DateTime.UtcNow,
                NotReadCount = notReadCount,
                ReadCount = readCount,
                Title = campaign.Title
            };
        }

        public async Task UpdateHit(Guid id) {
            DbContext.Hits.Add(new DbHit {
                CampaignId = id,
                TimeStamp = DateTimeOffset.UtcNow
            });
            await DbContext.SaveChangesAsync();
        }

        public async Task Publish(Guid id) {
            var campaign = await DbContext.Campaigns.FindAsync(id);
            if (campaign is null) {
                throw CampaignException.CampaignNotFound(id);
            }
            if (campaign.Published) {
                throw CampaignException.CampaignAlreadyPublished(id);
            }
            campaign.Published = true;
            await DbContext.SaveChangesAsync();
        }

        private async Task CreateMessages(CreateCampaignRequest request, Guid id) {
            if (!request.IsGlobal && request.Published) {
                if (request.SelectedUserCodes?.Count > 0) {
                    DbContext.Messages.AddRange(request.SelectedUserCodes.Select(userId => new DbMessage {
                        Id = Guid.NewGuid(),
                        RecipientId = userId,
                        CampaignId = id
                    }));
                    await DbContext.SaveChangesAsync();
                }
                if (request.DistributionListId is not null) {
                    var distributionList = await DbContext
                        .DistributionLists
                        .Include(x => x.Contacts)
                        .SingleOrDefaultAsync(x => x.Id == request.DistributionListId);
                    if (distributionList.Contacts.Any()) {
                        DbContext.Messages.AddRange(distributionList.Contacts.Select(contact => new DbMessage {
                            Id = Guid.NewGuid(),
                            RecipientId = contact.RecipientId,
                            CampaignId = id
                        }));
                    }
                    await DbContext.SaveChangesAsync();
                }
            }
        }
    }
}
