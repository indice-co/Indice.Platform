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
        _baseDir = Path.Join(env.ContentRootPath, "Prompts");
    }

    /// <inheritdoc/>
    public string Render(string templateName, object? values = null) {
        var compiled = _cache.GetOrAdd(templateName, name => {
            var path = Path.Join(_baseDir, $"{name}.txt");
            if (!File.Exists(path)) {
                return templateName switch {
                    nameof(AgentsConstants.PromptDefaults.AnswerComposer) => _handlebars.Compile(AgentsConstants.PromptDefaults.AnswerComposer),
                    nameof(AgentsConstants.PromptDefaults.Reranker) => _handlebars.Compile(AgentsConstants.PromptDefaults.Reranker),
                    nameof(AgentsConstants.PromptDefaults.IntentClassifier) => _handlebars.Compile(AgentsConstants.PromptDefaults.IntentClassifier),
                    nameof(AgentsConstants.PromptDefaults.PurposeResponder) => _handlebars.Compile(AgentsConstants.PromptDefaults.PurposeResponder),
                    nameof(AgentsConstants.PromptDefaults.QueryRewriter) => _handlebars.Compile(AgentsConstants.PromptDefaults.QueryRewriter),
                    
                    nameof(AgentsConstants.PromptDefaults.CaseRetriever) => _handlebars.Compile(AgentsConstants.PromptDefaults.CaseRetriever),
                    nameof(AgentsConstants.PromptDefaults.OtpCodeSenderInstructions) => _handlebars.Compile(AgentsConstants.PromptDefaults.OtpCodeSenderInstructions),
                    nameof(AgentsConstants.PromptDefaults.OtpCodeSenderPrompt) => _handlebars.Compile(AgentsConstants.PromptDefaults.OtpCodeSenderPrompt),
                    nameof(AgentsConstants.PromptDefaults.OtpCodeValidatorInstructions) => _handlebars.Compile(AgentsConstants.PromptDefaults.OtpCodeValidatorInstructions),
                    nameof(AgentsConstants.PromptDefaults.OtpCodeValidatorPrompt) => _handlebars.Compile(AgentsConstants.PromptDefaults.OtpCodeValidatorPrompt),
                    
                    _ => throw new InvalidOperationException($"Prompt template '{name}' not found at '{path}'."),
                };
            }
            return _handlebars.Compile(File.ReadAllText(path));
        });
        return compiled(values ?? new { });
    }
}
