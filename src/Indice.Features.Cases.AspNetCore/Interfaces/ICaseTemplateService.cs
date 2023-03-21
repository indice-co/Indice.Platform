using Indice.Features.Cases.Models.Responses;

namespace Indice.Features.Cases.Interfaces;

/// <summary>The template service for managing case templates</summary>
public interface ICaseTemplateService
{
    /// <summary>Create a html string from a razor template. The template convention is "Views/Cases/Pdf/{caseTypeCode}".</summary>
    /// <param name="case">The case to render.</param>
    /// <returns></returns>
    Task<string> RenderTemplateAsync(Case @case);
}
