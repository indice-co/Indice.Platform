#if NET7_0_OR_GREATER
#nullable enable

using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Messages.AspNetCore.Endpoints;

internal static class TemplatesHandlers
{
    public static async Task<Ok<ResultSet<TemplateListItem>>> GetTemplates([AsParameters] ListOptions options, ITemplateService templateService) {
        var templates = await templateService.GetList(options);
        return TypedResults.Ok(templates);
    }

    public static async Task<Results<Ok<Template>, NotFound>> GetTemplateById(Guid templateId, ITemplateService templateService) {
        var template = await templateService.GetById(templateId);
        if (template is null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(template);
    }

    public static async Task<Results<Created<Template>, ValidationProblem>> CreateTemplate(CreateTemplateRequest request, ITemplateService templateService) {
        var createdTemplate = await templateService.Create(request);
        return TypedResults.Created($"/templates/{createdTemplate.Id}", createdTemplate);
    }

    public static async Task<Results<NoContent, ValidationProblem>> UpdateTemplate(Guid templateId, UpdateTemplateRequest request, ITemplateService templateService) {
        await templateService.Update(templateId, request);
        return TypedResults.NoContent();
    }

    public static async Task<Results<NoContent, ValidationProblem>> DeleteTemplate(Guid templateId, ITemplateService templateService) {
        await templateService.Delete(templateId);
        return TypedResults.NoContent();
    }

    #region Descriptions

    public static readonly string GET_TEMPLATES_DESCRIPTION = @"
    Retrieves a list of available templates.
    
    Parameters:
    - options: List parameters including sorting, searching, page number, and page size.
    ";

    public static readonly string GET_TEMPLATE_BY_ID_DESCRIPTION = @"
    Retrieves a template by its unique ID.
    
    Parameters:
    - templateId: The unique identifier of the template.
    ";

    public static readonly string CREATE_TEMPLATE_DESCRIPTION = @"
    Creates a new template.
    
    Parameters:
    - request: Information about the template to be created.
    ";

    public static readonly string UPDATE_TEMPLATE_DESCRIPTION = @"
    Updates an existing template.
    
    Parameters:
    - templateId: The unique identifier of the template.
    - request: Information to update the template.
    ";

    public static readonly string DELETE_TEMPLATE_DESCRIPTION = @"
    Deletes a template permanently.
    
    Parameters:
    - templateId: The unique identifier of the template.
    ";

    #endregion
}

#nullable disable
#endif