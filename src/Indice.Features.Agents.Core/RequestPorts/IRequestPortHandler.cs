using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.RequestPorts;

/// <summary>Handles a workflow request-port event resolved by port id.</summary>
public interface IRequestPortHandler
{
    /// <summary>Handles a <see cref="RequestInfoEvent"/> and mutates run/state through the provided context.</summary>
    ValueTask<RequestPortHandlerResult> HandleAsync(RequestInfoEvent requestInfoEvent, RequestPortHandlerContext context, CancellationToken cancellationToken = default);
}

/// <summary>Mutable state used while handling a request-port event.</summary>
public sealed class RequestPortHandlerContext
{
    /// <summary>The currently running workflow stream.</summary>
    public required StreamingRun Run { get; init; }

    /// <summary>The conversation id associated with this stream.</summary>
    public required string ConversationId { get; init; }

    /// <summary>User reply available for resumed runs; handlers consume it when they answer a pending request.</summary>
    public string? UserReply { get; set; }
}

/// <summary>Result of request-port handling.</summary>
public sealed record RequestPortHandlerResult(bool Handled, bool ShouldHalt, IReadOnlyList<ChatResponseUpdate>? Updates = null)
{
    /// <summary>Result used when a handler cannot process the given request event.</summary>
    public static RequestPortHandlerResult NotHandled { get; } = new(false, false);

    /// <summary>Result used when request was processed and stream should continue.</summary>
    public static RequestPortHandlerResult Continue { get; } = new(true, false);

    /// <summary>Creates a handled result that emits updates and stops the current stream turn.</summary>
    public static RequestPortHandlerResult Halt(params ChatResponseUpdate[] updates) => new(true, true, updates);
}
