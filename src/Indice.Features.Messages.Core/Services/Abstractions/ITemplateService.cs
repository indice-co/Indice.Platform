using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Types;

namespace Indice.Features.Messages.Core.Services.Abstractions
{
    /// <summary>A service that contains template related operations.</summary>
    public interface ITemplateService
    {
        /// <summary>Creates a new template.</summary>
        /// <param name="request">The request model used to create a new template.</param>
        Task<Template> Create(CreateTemplateRequest request);
        /// <summary>Gets a template by it's unique id.</summary>
        /// <param name="id">The id of the template.</param>
        Task<Template> GetById(Guid id);
        /// <summary>Gets a list of all available templates.</summary>
        /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        Task<ResultSet<TemplateListItem>> GetList(ListOptions options);
    }
}
