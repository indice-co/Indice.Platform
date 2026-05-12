using HandlebarsDotNet;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;

namespace Indice.Features.Messages.Core.Rendering;

internal sealed class DbBackedPartialTemplateResolver : IPartialTemplateResolver
{
    private readonly ITemplateService _templateService;
    private readonly string _channel;

    /// <summary>
    /// Creates a new instance of <see cref="DbBackedPartialTemplateResolver"/> with the provided template service and message channel.
    /// </summary>
    /// <param name="templateService"></param>
    /// <param name="channel"></param>
    public DbBackedPartialTemplateResolver(ITemplateService templateService, string channel) {
        _templateService = templateService;
        _channel = channel;
    }
    /// <inheritdoc />
    public bool TryRegisterPartial(IHandlebars handlebars, string partialName, string templatePath) {
        var template = LoadTemplate(partialName);
        if (template == null) {
            handlebars.RegisterTemplate(partialName, string.Empty);
            return true;
        }
        if (template.Type != TemplateType.Partial && template.Type != TemplateType.Layout) {
            handlebars.RegisterTemplate(partialName, string.Empty);
            return true;
        }
        if (!template.Content.TryGetValue(_channel, out var content) || string.IsNullOrEmpty(content.Body)) {
            handlebars.RegisterTemplate(partialName, string.Empty);
            return true;
        }

        handlebars.RegisterTemplate(partialName, content.Body);
        return true;
    }

    /// <summary>
    /// Fetch the corresponding template from the database using the provided alias.
    /// </summary>
    /// <param name="alias"></param>
    /// <returns></returns>
    private Template? LoadTemplate(string alias) {
        return _templateService.GetById((GuidOrAlias)alias).ConfigureAwait(false).GetAwaiter().GetResult();
    }
}
