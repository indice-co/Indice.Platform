using HandlebarsDotNet;

namespace Indice.Features.Messages.Core.Rendering;

/// <summary>Factory for creating <see cref="IPartialTemplateResolver"/> instances per channel.</summary>
public interface IPartialTemplateResolverFactory
{
    /// <summary>Creates a new <see cref="IPartialTemplateResolver"/> for the specified channel.</summary>
    /// <param name="channel">The message channel.</param>
    IPartialTemplateResolver Create(string channel);
}
