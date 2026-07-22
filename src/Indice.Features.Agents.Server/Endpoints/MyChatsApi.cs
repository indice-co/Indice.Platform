using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Server;
using Indice.Features.Agents.Server.Endpoints;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>HTTP surface for the caller's chat sessions: create with first question, post follow-ups, list, get, delete.</summary>
internal static class MyChatsApi
{
    /// <summary>Maps the <c>/api/my/chats</c> endpoint group.</summary>
    public static RouteGroupBuilder MapMyChats(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<AgentsServerOptions>>().Value;
        var allowedScopes = new[] { options.ChatRequiredScope }.FilterOutNulls().ToArray();

        var group = routes.MapGroup($"{options.PathPrefix.Value?.TrimEnd('/')}/my/chats")
                          .WithName(options.GroupName)
                          .WithTags("Chats");

        group.RequireAuthorization(pb => pb.RequireAuthenticatedUser()
                                           .RequireClaim(BasicClaimTypes.Scope, allowedScopes));
        group.WithOpenApiSecurityRequirement("oauth2", allowedScopes);
        group.WithHandledException<BusinessException>()
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost(string.Empty, MyChatsHandlers.Create)
             .WithParameterValidation<ChatRequest>()
             .WithName(nameof(MyChatsHandlers.Create))
             .WithSummary("Create a chat session with the first question.")
             .WithDescription("Creates a new chat session for the calling user and runs the first question through the RAG pipeline in one round-trip. Returns 201 with the grounded answer, citations, usage, and the new session id.");

        group.MapPost("{chatId:guid}/messages", MyChatsHandlers.SendMessage)
             .WithParameterValidation<ChatRequest>()
             .WithName(nameof(MyChatsHandlers.SendMessage))
             .WithSummary("Post a follow-up message to an existing chat session.")
             .WithDescription("Loads bounded conversation history, runs the message through the RAG pipeline, persists the user/assistant turn, and returns the grounded answer + cumulative session token totals.");

        group.MapPost("stream", MyChatsHandlers.StreamCreate)
             .WithParameterValidation<ChatRequest>()
             .WithName(nameof(MyChatsHandlers.StreamCreate))
             .WithSummary("Create a chat session and stream the first turn over Server-Sent Events.")
             .WithDescription("""
                Streaming counterpart of `POST /api/my/chats`. Creates a session and streams the first turn as
                `text/event-stream` frames of the Dex message/delta protocol. Answer text rides the `delta` SSE event
                (`{"type":"delta","text":"…"}` — append chunks in arrival order); every other frame rides the default
                `message` event, discriminated by `type`:

                - `start` — first frame; carries the `conversationId` of the session.
                - `status` — pipeline progress label; ephemeral UI hint.
                - `citations` / `sources` — discrete parts of the completed answer; emitted only when non-empty.
                - `usage` — token totals and question counters for the turn.
                - `done` — terminal on success; carries the persisted assistant `messageId`, `responseId`, `modelId`,
                  `createdAt`, `finishReason` and `limitReached`. Limit-blocked turns stream the same grammar with the
                  predefined limit reply as a single delta and `finishReason` `limit` (nothing persisted).
                - `error` — terminal on failure; a safe generic `reason` (the question is recorded and counted; no
                  answer is persisted).

                The turn is persisted before the completion frames; assembling the deltas plus the part frames yields the
                same data as the non-streaming response. (SwaggerUI cannot render SSE — use a streaming client, e.g. `curl -N`.)
                """);

        group.MapPost("{chatId:guid}/messages/stream", MyChatsHandlers.StreamMessage)
             .WithParameterValidation<ChatRequest>()
             .WithName(nameof(MyChatsHandlers.StreamMessage))
             .WithSummary("Stream a follow-up turn over Server-Sent Events.")
             .WithDescription("""
                Streaming counterpart of `POST /api/my/chats/{chatId}/messages`. Emits the same message/delta frames as
                `POST /api/my/chats/stream` — `start`, `status` progress, `delta` text chunks, then the completion parts
                (`citations`/`sources`/`usage`) and the terminal `done` (or `error`). Returns 404 before any frame is sent
                when the session does not exist for the caller. (SwaggerUI cannot render SSE — use a streaming client,
                e.g. `curl -N`.)
                """);

        group.MapGet(string.Empty, MyChatsHandlers.List)
             .WithName(nameof(MyChatsHandlers.List))
             .WithSummary("List the caller's chat sessions, paged.")
             .WithDescription("Returns a paged list of sessions owned by the caller, ordered by most recent activity.");

        group.MapGet("{chatId:guid}", MyChatsHandlers.GetChatSession)
             .WithName(nameof(MyChatsHandlers.GetChatSession))
             .WithSummary("Get a chat session with its recent messages.")
             .WithDescription("Returns session metadata (title, timestamps, cumulative token totals) plus the most recent messages in chronological order.");

        group.MapDelete("{chatId:guid}", MyChatsHandlers.Delete)
             .WithName(nameof(MyChatsHandlers.Delete))
             .WithSummary("Delete a chat session and its messages.")
             .WithDescription("Permanently deletes the session and all its messages. Returns 204 on success, 404 when the session does not exist for the calling user.");

        group.MapPut("{chatId:guid}/messages/{messageId:guid}/like", MyChatsHandlers.Like)
             .WithParameterValidation<LikeRequest>()
             .WithName(nameof(MyChatsHandlers.Like))
             .WithSummary("Like or unlike a message in a chat session.")
             .WithDescription("Marks the message as liked or unliked by the caller. Returns 204 on success, 404 when the session or message does not exist for the calling user.");

        return group;
    }
}
