using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Exceptions;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Messages.Core.Services;

/// <summary>An implementation of <see cref="ITemplateService"/> for Entity Framework Core.</summary>
public class TemplateService : ITemplateService
{
    /// <summary>Creates a new instance of <see cref="DistributionListService"/>.</summary>
    /// <param name="dbContext">The <see cref="Microsoft.EntityFrameworkCore.DbContext"/> for Campaigns API feature.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public TemplateService(CampaignsDbContext dbContext) {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    private CampaignsDbContext DbContext { get; }

    /// <inheritdoc />
    public async Task<Template> Create(CreateTemplateRequest request) {
        var template = new DbTemplate {
            Content = request.Content,
            Id = Guid.NewGuid(),
            Name = request.Name,
            IgnoreUserPreferences = request.IgnoreUserPreferences,
            Data = request.Data,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        DbContext.Templates.Add(template);
        await DbContext.SaveChangesAsync();
        return new Template {
            Content = template.Content,
            IgnoreUserPreferences = request.IgnoreUserPreferences,
            Id = template.Id,
            Name = template.Name,
            Data= template.Data,
            CreatedAt= template.CreatedAt,
        };
    }

    /// <inheritdoc />
    public async Task Delete(Guid id) {
        var template = await DbContext.Templates.SingleOrDefaultAsync(x => x.Id == id);
        if (template is null) {
            return;
        }
        DbContext.Templates.Remove(template);
        await DbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<Template?> GetById(Guid? id) {
        var template = await DbContext.Templates.FindAsync(id);
        if (template is null) {
            return default;
        }
        return new Template {
            Content = template.Content,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt,
            UpdatedBy = template.UpdatedBy,
            CreatedBy = template.CreatedBy,
            Id = template.Id,
            Name = template.Name,
            IgnoreUserPreferences = template.IgnoreUserPreferences,
            Data = template.Data
        };
    }

    /// <inheritdoc />
    public async Task<ResultSet<TemplateListItem>> GetList(ListOptions options) {
        var query = DbContext.Templates.AsQueryable();
        if (!string.IsNullOrWhiteSpace(options.Search)) {
            query = query.Where(x => x.Name!.ToLower().Contains(options.Search.ToLower()));
        }
        var result = await query.ToResultSetAsync(options);
        var templateItems = result.Items.Select(x => new TemplateListItem {
            Channels = Enum.Parse<MessageChannelKind>(string.Join(',', x.Content.Select(x => x.Key)), ignoreCase: true),
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            UpdatedBy = x.UpdatedBy,
            CreatedBy = x.CreatedBy,
            Id = x.Id,
            Name = x.Name,
            IgnoreUserPreferences = x.IgnoreUserPreferences
        });
        return new ResultSet<TemplateListItem>(templateItems, result.Count);
    }

    /// <inheritdoc />
    public async Task Update(Guid id, UpdateTemplateRequest request) {
        var template = await DbContext.Templates.FindAsync(id);
        if (template is null) {
            throw MessageExceptions.TemplateNotFound(id);
        }
        template.Name = request.Name;
        template.Content = request.Content;
        template.Data = request.Data;
        template.UpdatedAt = DateTime.UtcNow;
        await DbContext.SaveChangesAsync();
    }
}
