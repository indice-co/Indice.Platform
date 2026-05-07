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
            Alias = string.IsNullOrWhiteSpace(request.Alias) ? null : request.Alias.Trim(),
            IgnoreUserPreferences = request.IgnoreUserPreferences,
            Data = request.Data,
            CreatedAt = DateTimeOffset.UtcNow,
            MessageTypeId = request.MessageTypeId,
            Type = request.Type
        };
        DbContext.Templates.Add(template);
        await DbContext.SaveChangesAsync();
        return new Template {
            Content = template.Content,
            IgnoreUserPreferences = request.IgnoreUserPreferences,
            Id = template.Id,
            Name = template.Name,
            Alias = template.Alias,
            Data = template.Data,
            CreatedAt = template.CreatedAt,
            MessageType = template.MessageType != null ? new MessageType {
                Id = template.MessageType.Id, 
                Name = template.MessageType.Name, 
                Alias = template.MessageType.Alias, 
                Classification = template.MessageType.Classification 
            } : null,
            Type = template.Type
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
    public async Task<Template?> GetById(GuidOrAlias? id) {
        if (id is null || id.Value == null) {
            return default;
        }
        DbTemplate? template = id.Value.IsGuid ?
            await DbContext.Templates.Include(x => x.MessageType).FirstOrDefaultAsync(x => x.Id == id.Value.Uuid) :
            await DbContext.Templates.Include(x => x.MessageType).FirstOrDefaultAsync(x => x.Alias == id.Value.Value);

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
            Alias = template.Alias,
            IgnoreUserPreferences = template.IgnoreUserPreferences,
            Data = template.Data,
            MessageType = template.MessageType != null ? new MessageType {
                Id = template.MessageType.Id,
                Name = template.MessageType.Name,
                Alias = template.MessageType.Alias,
                Classification = template.MessageType.Classification
            } : null,
            Type = template.Type
        };
    }

    /// <inheritdoc />
    public async Task<Template?> GetByAlias(string alias) {
        if (string.IsNullOrWhiteSpace(alias)) {
            return default;
        }
        var template = await DbContext.Templates.Include(x => x.MessageType).FirstOrDefaultAsync(x => x.Alias == alias.Trim());
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
            Alias = template.Alias,
            IgnoreUserPreferences = template.IgnoreUserPreferences,
            Data = template.Data,
            MessageType = template.MessageType != null ? new MessageType {
                Id = template.MessageType.Id,
                Name = template.MessageType.Name,
                Alias = template.MessageType.Alias,
                Classification = template.MessageType.Classification
            } : null,
            Type = template.Type
        };
    }

    /// <inheritdoc />
    public async Task<ResultSet<TemplateListItem>> GetList(ListOptions<TemplateListFilter> options) {
        var query = DbContext.Templates.Include(x => x.MessageType).AsQueryable();
        if (!string.IsNullOrWhiteSpace(options.Search) && options.Search.Length > 2) {
            query = query.Where(x =>
            x.Name!.ToLower().Contains(options.Search.ToLower()) ||
            x.Alias!.ToLower().Contains(options.Search.ToLower())
            );
        }

        if (options.Filter?.MessageTypeId is not null) {
            query = options.Filter.IncludeItemsWithoutMessageTypeId == true
                ? query.Where(x => x.MessageTypeId == options.Filter.MessageTypeId || x.MessageTypeId == null)
                : query.Where(x => x.MessageTypeId == options.Filter.MessageTypeId);
        } 
        if (options.Filter?.IncludeItemsWithoutMessageTypeId == false) {
            query = query.Where(x => x.MessageTypeId != null);
        }
        if (options.Filter?.Type is not null) {
            query = query.Where(x => x.Type == options.Filter.Type);
        }

        var result = await query.ToResultSetAsync(options);
        var templateItems = result.Items.Select(x => new TemplateListItem {
            Channels = x.Content.Select(x => Enum.Parse<MessageChannelKind>(x.Key, ignoreCase: true)).ToList(),
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            UpdatedBy = x.UpdatedBy,
            CreatedBy = x.CreatedBy,
            Id = x.Id,
            Name = x.Name,
            Alias = x.Alias,
            IgnoreUserPreferences = x.IgnoreUserPreferences,
            MessageType = x.MessageType != null ? new MessageType {
                Id = x.MessageType.Id,
                Name = x.MessageType.Name,
                Alias = x.MessageType.Alias,
                Classification =
                x.MessageType.Classification
            } : null,
            Type = x.Type
        });
        return new ResultSet<TemplateListItem>(templateItems, result.Count);
    }

    /// <inheritdoc />
    public async Task Update(Guid id, UpdateTemplateRequest request) {
        var template = await DbContext.Templates.FindAsync(id) ?? throw MessageExceptions.TemplateNotFound(id);
        template.Alias = string.IsNullOrWhiteSpace(request.Alias) ? null : request.Alias.Trim();
        if (!string.IsNullOrWhiteSpace(template.Alias)) {
            var existingAlias = await GetById((GuidOrAlias)template.Alias);
            if (existingAlias != null && existingAlias.Id != id) {
                throw MessageExceptions.TemplateAliasExists(template.Alias);
            }
        }

        template.Name = request.Name.Trim();
        template.Content = request.Content;
        template.Data = request.Data;
        template.UpdatedAt = DateTime.UtcNow;
        await DbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateIgnreUserPreferences(Guid id, bool ignoreUserPreferences) {
        var template = await DbContext.Templates.FindAsync(id);
        if (template is null) {
            throw MessageExceptions.TemplateNotFound(id);
        }
        template.IgnoreUserPreferences = ignoreUserPreferences;
        template.UpdatedAt = DateTime.UtcNow;
        await DbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByName(string name) => await DbContext.Templates.AnyAsync(x => x.Name.ToLower() == name.Trim().ToLower());

    /// <inheritdoc />
    public async Task UpdateMessageType(Guid id, Guid? messageTypeId) {
        var template = await DbContext.Templates.FindAsync(id) ?? throw MessageExceptions.TemplateNotFound(id);
        template.MessageTypeId = messageTypeId;
        template.UpdatedAt = DateTime.UtcNow;
        await DbContext.SaveChangesAsync();
    }
}
