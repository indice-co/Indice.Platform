using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Exceptions;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Indice.Features.Messages.Core.Services;

/// <summary>An implementation of <see cref="ICampaignService"/> for Entity Framework Core.</summary>
public class CampaignService : ICampaignService
{
    /// <summary>Creates a new instance of <see cref="CampaignService"/>.</summary>
    /// <param name="dbContext">The <see cref="Microsoft.EntityFrameworkCore.DbContext"/> for Campaigns API feature.</param>
    /// <param name="campaignManagementOptions">Options used to configure the Campaigns management API feature.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public CampaignService(
        CampaignsDbContext dbContext,
        IOptions<MessageManagementOptions> campaignManagementOptions
    ) {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        CampaignManagementOptions = campaignManagementOptions?.Value ?? throw new ArgumentNullException(nameof(campaignManagementOptions));
    }

    private CampaignsDbContext DbContext { get; }
    private MessageManagementOptions CampaignManagementOptions { get; }

    /// <inheritdoc />
    public Task<ResultSet<Campaign>> GetList(ListOptions<CampaignListFilter> options) {
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
        if (options.Filter.MessageChannelKind.HasValue) {
            query = query.Where(x => x.MessageChannelKind.HasFlag(options.Filter.MessageChannelKind.Value));
        }
        if (options.Filter.Published.HasValue) {
            query = query.Where(x => x.Published == options.Filter.Published.Value);
        }
        return query.ToResultSetAsync(options);
    }

    /// <inheritdoc />
    public async Task<CampaignDetails> GetById(Guid id) {
        var campaign = await DbContext
            .Campaigns
            .AsNoTracking()
            .Include(x => x.Attachment)
            .Include(x => x.Type)
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

    /// <inheritdoc />
    public async Task<Campaign> Create(CreateCampaignRequest request) {
        var dbCampaign = Mapper.ToDbCampaign(request);
        DbContext.Campaigns.Add(dbCampaign);
        await DbContext.SaveChangesAsync();
        return Mapper.ToCampaign(dbCampaign);
    }

    /// <inheritdoc />
    public async Task Update(Guid id, UpdateCampaignRequest request) {
        var campaign = await DbContext.Campaigns.SingleOrDefaultAsync(x => x.Id == id);
        if (campaign is null) {
            throw MessageExceptions.CampaignNotFound(id);
        }
        if (campaign.Published) {
            throw MessageExceptions.CampaignAlreadyPublished(id);
        }
        campaign.ActionLink = request.ActionLink;
        campaign.ActivePeriod = request.ActivePeriod;
        campaign.Content = request.Content;
        campaign.MessageChannelKind = Enum.Parse<MessageChannelKind>(string.Join(',', request.Content.Select(x => x.Key)), ignoreCase: true);
        campaign.Title = request.Title;
        campaign.TypeId = request.TypeId;
        campaign.DistributionListId = request.RecipientListId;
        campaign.Data = request.Data;
        await DbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task Delete(Guid id) {
        var campaign = await DbContext.Campaigns.FindAsync(id);
        if (campaign is null) {
            throw MessageExceptions.CampaignNotFound(id);
        }
        DbContext.Remove(campaign);
        await DbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task UpdateHit(Guid id) {
        DbContext.Hits.Add(new DbHit {
            CampaignId = id,
            TimeStamp = DateTimeOffset.UtcNow
        });
        await DbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<Campaign> Publish(Guid id) {
        var campaign = await DbContext
            .Campaigns
            .Include(x => x.Type)
            .Include(x => x.DistributionList)
            .SingleOrDefaultAsync(x => x.Id == id);
        if (campaign is null) {
            throw MessageExceptions.CampaignNotFound(id);
        }
        if (campaign.Published) {
            throw MessageExceptions.CampaignAlreadyPublished(id);
        }
        campaign.Published = true;
        await DbContext.SaveChangesAsync();
        return Mapper.ToCampaign(campaign);
    }
}
