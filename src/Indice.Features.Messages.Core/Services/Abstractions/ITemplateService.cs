using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;

namespace Indice.Features.Messages.Core.Services.Abstractions
{
    /// <summary>
    /// A service that contains template related operations.
    /// </summary>
    public interface ITemplateService
    {
        /// <summary>
        /// Creates a new template.
        /// </summary>
        /// <param name="request">The request model used to create a new template.</param>
        Task<Template> Create(CreateTemplateRequest request);
        /// <summary>
        /// Gets a template by it's unique id.
        /// </summary>
        /// <param name="id">The id of the template.</param>
        Task<Template> GetById(Guid id);
    }
}
