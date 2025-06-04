using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Services;

namespace Indice.Features.Cases.Core.Services;

/// <inheritdoc />
public class CaseTemplateService : ICaseTemplateService
{
    /// <inheritdoc />
    public CaseTemplateService(IHtmlRenderingEngine htmlRenderingEngine) {
        HtmlRenderingEngine = htmlRenderingEngine;
    }

    /// <summary>
    /// The html rendering engine for creating templates
    /// </summary>
    protected IHtmlRenderingEngine HtmlRenderingEngine { get; }

    /// <inheritdoc />
    public async Task<string> RenderTemplateAsync(Case @case) {
        return await HtmlRenderingEngine.RenderAsync($"Cases/Pdf/{@case.CaseType.Code}", @case);
    }
}
