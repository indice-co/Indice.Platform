namespace Indice.Features.Agents.Core.Workflows.Prompts;

/// <summary>
/// Renders a named prompt template against an arbitrary input object using Handlebars syntax (<c>{{var}}</c>, <c>{{#each}}</c>, <c>{{#if}}</c>).
/// </summary>
public interface IPromptTemplateRenderer
{
    /// <summary>Loads the template <paramref name="templateName"/> and renders it against <paramref name="values"/>. Handlebars dispatches on the object's public properties (anonymous objects, dictionaries, and typed DTOs all work).</summary>
    string Render(string templateName, object? values = null);
}
