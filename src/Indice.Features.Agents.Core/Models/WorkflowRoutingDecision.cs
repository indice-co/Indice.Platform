namespace Indice.Features.Agents.Core.Models;

/// <summary>Decision returned by dynamic workflow routing for an initial user message.</summary>
public sealed class WorkflowRoutingDecision
{
    /// <summary>The workflow key to execute (<c>knowledge</c> or <c>cases</c>).</summary>
    public required string WorkflowName { get; init; }

    /// <summary>Classifier confidence in the selected workflow, from 0 to 1.</summary>
    public double Confidence { get; init; }

    /// <summary>When true, the router requests clarification instead of starting a workflow run.</summary>
    public bool IsAmbiguous { get; init; }

    /// <summary>Optional short reason useful for diagnostics.</summary>
    public string? Reason { get; init; }

    /// <summary>Clarification prompt to return to the user when <see cref="IsAmbiguous"/> is true.</summary>
    public string? ClarificationPrompt { get; init; }

    /// <summary>Optional clarification choices to guide the user.</summary>
    public IReadOnlyList<string>? ClarificationOptions { get; init; }
}
