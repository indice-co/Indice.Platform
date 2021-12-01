using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Controllers;
using Indice.AspNetCore.Features.Campaigns.Data;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
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
            CampaingsDbContext dbContext,
            IOptions<GeneralSettings> generalSettings,
            IFileService fileService,
            IHttpContextAccessor httpContextAccessor,
            LinkGenerator linkGenerator
        ) {
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            FileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            GeneralSettings = generalSettings?.Value ?? throw new ArgumentNullException(nameof(generalSettings));
            HttpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            LinkGenerator = linkGenerator ?? throw new ArgumentNullException(nameof(linkGenerator));
        }

        public CampaingsDbContext DbContext { get; }
        public IFileService FileService { get; }
        public GeneralSettings GeneralSettings { get; }
        public IHttpContextAccessor HttpContextAccessor { get; }
        public LinkGenerator LinkGenerator { get; }

        public async Task AssociateCampaignAttachment(Guid campaignId, Guid attachmentId) {
            var campaign = await DbContext.Campaigns.FindAsync(campaignId);
            campaign.AttachmentId = attachmentId;
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
                Id = attachment.Id,
                FileGuid = attachment.Guid,
                ContentType = file.ContentType,
                Label = file.FileName,
                Size = file.Length,
                PermaLink = LinkGenerator.GetUriByAction(HttpContextAccessor.HttpContext, nameof(CampaignsController.GetCampaignAttachment), CampaignsController.Name, new {
                    fileGuid = (Base64Id)attachment.Guid,
                    format = Path.GetExtension(attachment.Name).TrimStart('.')
                })
            };
        }

        public async Task<Campaign> CreateCampaign(CreateCampaignRequest request) {
            var dbCampaign = request.ToDbCampaign();
            DbContext.Campaigns.Add(dbCampaign);
            if (!request.IsGlobal && request.SelectedUserCodes?.Count > 0) {
                var campaignUsers = request.SelectedUserCodes.Select(userId => new DbCampaignUser {
                    Id = Guid.NewGuid(),
                    UserCode = userId,
                    CampaignId = dbCampaign.Id
                });
                DbContext.CampaignUsers.AddRange(campaignUsers);
            }
            await DbContext.SaveChangesAsync();
            var campaign = dbCampaign.ToCampaign();
            return campaign;
        }

        public async Task<CampaignType> CreateCampaignType(CreateCampaignTypeRequest request) {
            var campaignType = new DbCampaignType {
                Id = Guid.NewGuid(),
                Name = request.Name
            };
            DbContext.CampaignTypes.Add(campaignType);
            await DbContext.SaveChangesAsync();
            return new CampaignType {
                Id = campaignType.Id,
                Name = campaignType.Name
            };
        }

        public async Task DeleteCampaign(Guid campaignId) {
            var campaign = await DbContext.Campaigns.FindAsync(campaignId);
            if (campaign is null) {
                return;
            }
            DbContext.Remove(campaign);
            await DbContext.SaveChangesAsync();
        }

        public async Task DeleteCampaignType(Guid campaignTypeId) {
            var campaignType = await DbContext.CampaignTypes.FindAsync(campaignTypeId);
            if (campaignType is null) {
                return;
            }
            DbContext.Remove(campaignType);
            await DbContext.SaveChangesAsync();
        }

        public Task<CampaignDetails> GetCampaignById(Guid campaignId) =>
            DbContext.Campaigns
                     .AsNoTracking()
                     .Include(x => x.Attachment)
                     .Select(campaign => new CampaignDetails {
                         ActionText = campaign.ActionText,
                         ActionUrl = campaign.ActionUrl,
                         ActivePeriod = campaign.ActivePeriod,
                         Attachment = campaign.Attachment != null ? new AttachmentLink {
                             Id = campaign.Attachment.Id,
                             FileGuid = campaign.Attachment.Guid,
                             ContentType = campaign.Attachment.ContentType,
                             Label = campaign.Attachment.Name,
                             Size = campaign.Attachment.ContentLength,
                             PermaLink = $"{GeneralSettings.Host.TrimEnd('/')}/api/campaigns/attachments/{(Base64Id)campaign.Attachment.Guid}.{Path.GetExtension(campaign.Attachment.Name).TrimStart('.')}"
                         } : null,
                         Content = campaign.Content,
                         CreatedAt = campaign.CreatedAt,
                         Id = campaign.Id,
                         IsActive = campaign.IsActive,
                         IsGlobal = campaign.IsGlobal,
                         Title = campaign.Title,
                         Type = campaign.Type != null ? new CampaignType {
                             Id = campaign.Type.Id,
                             Name = campaign.Type.Name
                         } : null
                     })
                     .SingleOrDefaultAsync(x => x.Id == campaignId);

        public Task<ResultSet<Campaign>> GetCampaigns(ListOptions options) =>
            DbContext.Campaigns
                     .AsNoTracking()
                     .Select(campaign => new Campaign {
                         ActionText = campaign.ActionText,
                         ActionUrl = campaign.ActionUrl,
                         ActivePeriod = campaign.ActivePeriod,
                         Content = campaign.Content,
                         CreatedAt = campaign.CreatedAt,
                         Id = campaign.Id,
                         IsActive = campaign.IsActive,
                         IsGlobal = campaign.IsGlobal,
                         Title = campaign.Title
                     })
                     .ToResultSetAsync(options);

        public async Task<CampaignStatistics> GetCampaignStatistics(Guid campaignId) {
            var campaign = await DbContext.Campaigns.FindAsync(campaignId);
            if (campaign is null) {
                return default;
            }
            var clickToActionCount = await DbContext.CampaignVisits.AsNoTracking().CountAsync(x => x.CampaignId == campaignId);
            var readCount = await DbContext.CampaignUsers.AsNoTracking().CountAsync(x => x.CampaignId == campaignId && x.IsRead);
            var deletedCount = await DbContext.CampaignUsers.AsNoTracking().CountAsync(x => x.CampaignId == campaignId && x.IsDeleted);
            int? notReadCount = null;
            if (!campaign.IsGlobal) {
                notReadCount = await DbContext.CampaignUsers.AsNoTracking().CountAsync(x => x.CampaignId == campaignId && !x.IsRead);
            }
            return new CampaignStatistics {
                ClickToActionCount = clickToActionCount,
                DeletedCount = deletedCount,
                LastUpdated = DateTime.UtcNow,
                NotReadCount = notReadCount,
                ReadCount = readCount,
                Title = campaign.Title
            };
        }

        public async Task<CampaignType> GetCampaignTypeById(Guid campaignTypeId) {
            var campaign = await DbContext.CampaignTypes.FindAsync(campaignTypeId);
            if (campaign is null) {
                return default;
            }
            return new CampaignType {
                Id = campaign.Id,
                Name = campaign.Name
            };
        }

        public Task<ResultSet<CampaignType>> GetCampaignTypes(ListOptions options) =>
            DbContext.CampaignTypes
                     .AsNoTracking()
                     .Select(campaignType => new CampaignType {
                         Id = campaignType.Id,
                         Name = campaignType.Name
                     })
                     .ToResultSetAsync(options);

        public async Task UpdateCampaign(Guid campaignId, UpdateCampaignRequest request) {
            var campaign = await DbContext.Campaigns.FindAsync(campaignId);
            if (campaign is null) {
                return;
            }
            campaign.ActionText = request.ActionText;
            campaign.ActivePeriod = request.ActivePeriod;
            campaign.Content = request.Content;
            campaign.Title = request.Title;
            campaign.IsActive = request.IsActive;
            await DbContext.SaveChangesAsync();
        }

        public async Task UpdateCampaignVisit(Guid campaignId) {
            DbContext.CampaignVisits.Add(new DbCampaignVisit {
                CampaignId = campaignId,
                TimeStamp = DateTimeOffset.UtcNow
            });
            await DbContext.SaveChangesAsync();
        }
    }
}
