using Indice.Features.Agents.Core.Models;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Extensions;


/// <summary>
/// Provides extension methods for the workflow context and chat messages.
/// </summary>
public static class WorkflowExtensions
{
    /// <summary>
    /// Extension methods for the <see cref="IWorkflowContext"/> interface.
    /// </summary>
    /// <param name="context">The workflow context.</param>
    extension(IWorkflowContext context)
    {
        /// <summary>
        /// Sends a message as the assistant via the workflow context that surfaces to the workflow runner as an <see cref="AgentResponseUpdateEvent"/>.
        /// </summary>
        /// <remarks>Used to bubble up messages from the assistant to the workflow runner and eventually to the client.</remarks>
        /// <param name="executorId">The executor ID.</param>
        /// <param name="message">The message to send.</param>
        /// <returns>A value task representing the asynchronous operation.</returns>
        public ValueTask Say(string executorId, string message)
            => context.AddEventAsync(new AgentResponseUpdateEvent(executorId, new AgentResponseUpdate(ChatRole.Assistant, [new TextContent(message)])));
    }

    /// <summary>
    /// Extension methods for the <see cref="RequestInfoEvent"/> class.
    /// </summary>
    /// <param name="requestInfoEvent">The request info event.</param>
    extension(RequestInfoEvent requestInfoEvent)
    {
        /// <summary>
        /// Creates an <see cref="AgentResponseUpdate"/> from a <see cref="RequestInfoEvent"/> and a <see cref="CheckpointInfo"/>.
        /// </summary>
        /// <param name="checkpoint">The checkpoint info.</param>
        /// <returns>The agent response update.</returns>
        public AgentResponseUpdate AsAgentResponseUpdate(CheckpointInfo checkpoint)
            => new (ChatRole.Assistant, [new FunctionCallContent(new AgentFunctionCallId(requestInfoEvent.Request.RequestId, checkpoint.CheckpointId), requestInfoEvent.Request.PortInfo.RequestType.TypeName, new Dictionary<string, object?>() { ["data"] = requestInfoEvent.Request.Data })]);
    }

    /// <summary>
    /// Extension methods for the <see cref="ChatMessage"/> class.
    /// </summary>
    /// <param name="chatMessage">The chat message.</param>
    extension(ChatMessage chatMessage)
    {
        /// <summary>
        /// Gets the function result from a <see cref="ChatMessage"/>.
        /// </summary>
        /// <returns>The function result.</returns>
        public object? GetFunctionResult()
            => chatMessage.Contents.OfType<FunctionResultContent>().FirstOrDefault()?.Result;

        /// <summary>
        /// Gets the function result content from a <see cref="ChatMessage"/>.
        /// </summary>
        /// <returns>The function result content.</returns>
        public FunctionResultContent? GetFunctionResultContent(string? requestId = null)
            => chatMessage.Contents.OfType<FunctionResultContent>()
                                   .FirstOrDefault(c => requestId == null || (AgentFunctionCallId.TryParse(c.CallId, out var callId) && callId!.RequestId.Equals(requestId)));

        /// <summary>
        /// Tries to get the function result content from a <see cref="ChatMessage"/>.
        /// </summary>
        /// <param name="requestId">The optional request ID to match.</param>
        /// <param name="functionResultContent">When this method returns, contains the function result content if the parsing succeeded, or null if the parsing failed.</param>
        /// <returns>True if the parsing succeeded; otherwise, false.</returns>
        public bool TryGetFunctionResultContent(string? requestId, out FunctionResultContent? functionResultContent) {
            functionResultContent = GetFunctionResultContent(chatMessage, requestId);
            return functionResultContent != null;
        }

        /// <summary>
        /// Determines whether a <see cref="ChatMessage"/> has function result content.
        /// </summary>
        /// <param name="requestId">The optional request ID to match.</param>
        /// <returns><c>true</c> if the chat message has function result content; otherwise, <c>false</c>.</returns>
        public bool HasFunctionResultContent(string? requestId = null)
            => GetFunctionResultContent(chatMessage, requestId) != null;

        /// <summary>
        /// Determines whether a <see cref="ChatMessage"/> has function result content.
        /// </summary>
        /// <param name="requestId">The optional request ID to match.</param>
        /// <returns><c>true</c> if the chat message has function result content; otherwise, <c>false</c>.</returns>
        public AgentFunctionCallId GetFunctionResultContentCallId(string? requestId = null) =>
            AgentFunctionCallId.Parse(GetFunctionResultContent(chatMessage, requestId)?.CallId ?? throw new InvalidOperationException("Function result content not found."));

    }
}