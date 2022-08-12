using Indice.EntityFrameworkCore.Functions;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;

namespace Indice.Features.Messages.Core.Services
{
    /// <summary>
    /// An implementation of <see cref="ITemplateService"/> for Entity Framework Core.
    /// </summary>
    public class TemplateService : ITemplateService
    {
        /// <summary>
        /// Creates a new instance of <see cref="DistributionListService"/>.
        /// </summary>
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
                CreatedAt = DateTimeOffset.UtcNow,
                Id = Guid.NewGuid(),
                Name = request.Name
            };
            DbContext.Templates.Add(template);
            await DbContext.SaveChangesAsync();
            return new Template {
                Content = template.Content,
                Id = template.Id,
                Name = template.Name
            };
        }

        /// <inheritdoc />
        public async Task<Template> GetById(Guid id) {
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
                Name = template.Name
            };
        }

        /// <inheritdoc />
        public async Task<ResultSet<TemplateListItem>> GetList(ListOptions options) {
            var query = DbContext.Templates.AsQueryable();
            if (!string.IsNullOrWhiteSpace(options.Search)) {
                query = query.Where(x => x.Name.ToLower().Contains(options.Search.ToLower()));
            }
            var result = await query.ToResultSetAsync(options);
            var templateItems = result.Items.Select(x => new TemplateListItem {
                Channels = Enum.Parse<MessageChannelKind>(string.Join(',', x.Content.Select(x => x.Key)), ignoreCase: true),
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                UpdatedBy = x.UpdatedBy,
                CreatedBy = x.CreatedBy,
                Id = x.Id,
                Name = x.Name
            });
            return new ResultSet<TemplateListItem>(templateItems, result.Count);
        }
    }
}
