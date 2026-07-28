namespace Indice.Features.Agents.Core.Workflows.Mcp;

/// <summary>
/// Terminal output of any LLM agent step that streams a free-text answer to the caller.
/// </summary>
public sealed class RagPipelineOutput
{
    /// <summary>The fully accumulated answer text produced by the model.</summary>
    public string Answer { get; init; } = string.Empty;
}
