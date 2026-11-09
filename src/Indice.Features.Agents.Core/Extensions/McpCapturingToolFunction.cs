using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Extensions;

/// <summary>
/// Captures the raw tool output and terminates the function-invocation loop,
/// so the (potentially large, e.g. base64 images) payload is never sent back to the model.
/// </summary>
public sealed class McpCapturingToolFunction(AIFunction inner) : DelegatingAIFunction(inner)
{
    /// <inheritdoc />
    public object? Result { get; private set; }

    /// <inheritdoc />
    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken) {
        Result = await base.InvokeCoreAsync(arguments, cancellationToken);
        if (FunctionInvokingChatClient.CurrentContext is { } invocationContext) {
            invocationContext.Terminate = true;
        }
        return "Case data retrieved.";
    }
}
