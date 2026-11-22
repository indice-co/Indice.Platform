namespace Indice.Features.Agents.Core.Workflows.Prompts;

/// <summary>
/// The default text of a prompt template contributed by an agent registered outside this package. The host still overrides it
/// with <c>Prompts/{Name}.txt</c>. Register it with <c>services.AddPromptTemplateDefault(name, template)</c>.
/// </summary>
/// <param name="Name">The template name passed to <see cref="IPromptTemplateRenderer.Render(string, object?)"/>.</param>
/// <param name="Template">The Handlebars template.</param>
public sealed record PromptTemplateDefault(string Name, string Template);
