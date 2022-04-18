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
                Id = template.Id,
                Name = template.Name
            };
        }

        /// <inheritdoc />
        public async Task<ResultSet<Template>> GetList(ListOptions options) {
            var query = DbContext.Templates.Select(x => new Template {
                Content = x.Content,
                Id = x.Id,
                Name = x.Name
            });
            if (!string.IsNullOrWhiteSpace(options.Search)) {
                query = query.Where(x => x.Name.ToLower().Contains(options.Search.ToLower()));
            }
            return await query.ToResultSetAsync(options);
        }
    }
}
