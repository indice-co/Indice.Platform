using System.Diagnostics;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core;

/// <summary>
/// Ephemeral <see cref="AIContent"/> used to surface workflow step progress to streaming consumers (e.g. UI updates).
/// Instances are stripped from the final composed <see cref="ChatResponse"/> and are never persisted.
/// </summary>
/// <param name="label">The human readable label of the workflow step currently executing.</param>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class StepProgressContent(string label) : AIContent
{
    /// <summary>The human readable label of the workflow step currently executing.</summary>
    public string Label { get; } = label;

    //
    // Summary:
    //     Gets a string representing this instance to display in the debugger.
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => "Step = \"" + Label + "\"";

}
