using System.Collections.Concurrent;
using HandlebarsDotNet;
using Microsoft.Extensions.Hosting;

namespace Indice.Features.Agents.Core.Workflows.Prompts;

/// <summary>
/// Default <see cref="IPromptTemplateRenderer"/> that loads templates from <c>{ContentRoot}/Prompts/{name}.txt</c>
/// and caches the compiled Handlebars delegate keyed by template name. One <see cref="IHandlebars"/> instance per renderer.
/// </summary>
public sealed class FileSystemPromptTemplateRenderer : IPromptTemplateRenderer
{
    private readonly string _baseDir;
    private readonly IHandlebars _handlebars = Handlebars.Create();
    private readonly ConcurrentDictionary<string, HandlebarsTemplate<object, object>> _cache = new();

    /// <summary>Creates a new <see cref="FileSystemPromptTemplateRenderer"/>.</summary>
    public FileSystemPromptTemplateRenderer(IHostEnvironment env) {
        _baseDir = Path.Combine(env.ContentRootPath, "Prompts");
    }

    /// <inheritdoc/>
    public string Render(string templateName, object? values = null) {
        var compiled = _cache.GetOrAdd(templateName, name => {
            var path = Path.Combine(_baseDir, $"{name}.txt");
            if (!File.Exists(path)) {
                throw new InvalidOperationException($"Prompt template '{name}' not found at '{path}'.");
            }
            return _handlebars.Compile(File.ReadAllText(path));
        });
        return compiled(values ?? new { });
    }
}
