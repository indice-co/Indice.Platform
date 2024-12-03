using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;

namespace Indice.Features.Cases.Core.Services;

/// <inheritdoc />
public class NoOpCaseTemplateService : ICaseTemplateService
{
    /// <inheritdoc />
    public Task<string> RenderTemplateAsync(Case @case) {
        //TODO: this is probably a host application service from the server project. and should be different according to the technology.
        throw new NotImplementedException();
    }
}
