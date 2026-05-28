using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Types;

namespace Indice.Features.Messages.Core.Services.Abstractions;

/// <summary>A service that contains template related operations.</summary>
public interface ITemplateService
{
    /// <summary>Creates a new template.</summary>
    /// <param name="request">The request model used to create a new template.</param>
    Task<Template> Create(CreateTemplateRequest request);
    /// <summary>Gets a template by it's unique id.</summary>
    /// <param name="id">The id of the template.</param>
    Task<Template?> GetById(GuidOrAlias? id);
    /// <summary>Gets every template whose <see cref="TemplateType"/> is <see cref="TemplateType.Partial"/> or <see cref="TemplateType.Layout"/>, with full <see cref="Template.Content"/>. Intended for client-side Handlebars partial registration during preview.</summary>
    Task<ResultSet<Template>> GetPartialsAndLayouts();
    /// <summary>Gets a list of all available templates.</summary>
    /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
    Task<ResultSet<TemplateListItem>> GetList(ListOptions<TemplateListFilter> options);
    /// <summary>Permanently deletes a template from the store.</summary>
    /// <param name="id">The id of the template.</param>
    Task Delete(Guid id);
    /// <summary>Updates an existing template.</summary>
    /// <param name="id">The id of the template.</param>
    /// <param name="request">The request model used to update an existing template.</param>
    Task Update(Guid id, UpdateTemplateRequest request);
    /// <summary>Updates flag Ignore User Preference to an existing template.</summary>
    /// <param name="id">The id of the template.</param>
    /// <param name="ignoreUserPreferences">Value for flag IgnoreUserPreferences.</param>
    Task UpdateIgnreUserPreferences(Guid id, bool ignoreUserPreferences);

    /// <summary>Checks if a template with the provided name exists</summary>
    /// <param name="name">The name of the template.</param>
    Task<bool> ExistsByName(string name);
    /// <summary>Updates flag Ignore User Preference to an existing template.</summary>
    /// <param name="id">The id of the template.</param>
    /// <param name="messageTypeId">The id for template.</param>
    Task UpdateMessageType(Guid id, Guid? messageTypeId);
}
