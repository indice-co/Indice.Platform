using HandlebarsDotNet;
using Indice.Features.Messages.Core.Services.Abstractions;

namespace Indice.Features.Messages.Core.Rendering;

/// <summary>Default implementation of <see cref="IPartialTemplateResolverFactory"/> backed by <see cref="DbBackedPartialTemplateResolver"/>.</summary>
public sealed class DbBackedPartialTemplateResolverFactory : IPartialTemplateResolverFactory
{
    private readonly ITemplateService _templateService;

    /// <summary>Creates a new instance of <see cref="DbBackedPartialTemplateResolverFactory"/>.</summary>
    /// <param name="templateService">A service that contains template related operations.</param>
    public DbBackedPartialTemplateResolverFactory(ITemplateService templateService) {
        _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
    }

    /// <inheritdoc />
    public IPartialTemplateResolver Create(string channel) => new DbBackedPartialTemplateResolver(_templateService, channel);
}
