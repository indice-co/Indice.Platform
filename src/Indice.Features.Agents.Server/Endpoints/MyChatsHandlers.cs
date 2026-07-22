using System.Security.Claims;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Services;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Agents.Server.Endpoints;

/// <summary>Logic-free handlers (command and query invokers).</summary>
/// <remarks>Their shape will enforce open api documentation without the need to add explicit annotations and metadata. 
/// So Union types are used to represent multiple possible outcomes and <see cref="TypedResults"/> are always prefered over raw results.</remarks>
internal static class MyChatsHandlers
{
    /// <summary>POST /api/my/chats — creates a conversation with the first question.</summary>
    public static async Task<CreatedAtRoute<DexChatResponse>> Create(ChatRequest request, ClaimsPrincipal user, IChatsService chats, CancellationToken cancellationToken) {
        var userId = user.FindSubjectId()!;
        var response = await chats.SendAsync(userId, conversationId: null, request, cancellationToken);
        return TypedResults.CreatedAtRoute(response, nameof(GetChatSession), new { chatId = response!.ConversationId });
    }

    /// <summary>POST /api/my/chats/{chatId}/messages — posts a follow-up turn.</summary>
    public static async Task<Results<Ok<DexChatResponse>, NotFound>> SendMessage(Guid chatId, ChatRequest request, ClaimsPrincipal user,
        IChatsService chats, CancellationToken cancellationToken) {
        var userId = user.FindSubjectId()!;
        var response = await chats.SendAsync(userId, chatId, request, cancellationToken);
        return response is null ? TypedResults.NotFound() : TypedResults.Ok(response);
    }

    /// <summary>POST /api/my/chats/stream — creates a conversation and streams the first turn over SSE.</summary>
    public static async Task<ServerSentEventsResult<DexChatResponseUpdate>> StreamCreate(ChatRequest request, ClaimsPrincipal user,
        IChatsService chats, CancellationToken cancellationToken) {
        var userId = user.FindSubjectId()!;
        // conversationId null ⇒ the conversation is created inline, so the stream is never null here.
        var stream = await chats.SendStreamAsync(userId, conversationId: null, request.Text, cancellationToken);
        return TypedResults.ServerSentEvents(stream!);
    }

    /// <summary>POST /api/my/chats/{chatId}/messages/stream — streams a follow-up turn over SSE.</summary>
    public static async Task<Results<ServerSentEventsResult<DexChatResponseUpdate>, NotFound>> StreamMessage(Guid chatId, ChatRequest request,
        ClaimsPrincipal user, IChatsService chats, CancellationToken cancellationToken) {
        var userId = user.FindSubjectId()!;
        var stream = await chats.SendStreamAsync(userId, chatId, request.Text, cancellationToken);
        return stream is null ? TypedResults.NotFound() : TypedResults.ServerSentEvents(stream);
    }

    /// <summary>GET /api/my/chats — paged list of the caller's sessions.</summary>
    public static async Task<Ok<ResultSet<ConversationListItem>>> List([AsParameters] ListOptions options, ClaimsPrincipal user,
        IChatsService chats, CancellationToken cancellationToken)
        => TypedResults.Ok(await chats.ListAsync(user.FindSubjectId()!, options, cancellationToken));

    /// <summary>GET /api/my/chats/{chatId} — conversation detail with recent messages.</summary>
    public static async Task<Results<Ok<DexConversation>, NotFound>> GetChatSession(Guid chatId, ClaimsPrincipal user, IChatsService chats, CancellationToken cancellationToken) {
        var conversation = await chats.GetAsync(user.FindSubjectId()!, chatId, cancellationToken);
        return conversation is null ? TypedResults.NotFound() : TypedResults.Ok(conversation);
    }

    /// <summary>DELETE /api/my/chats/{chatId} — removes a conversation and its messages.</summary>
    public static async Task<Results<NoContent, NotFound>> Delete(Guid chatId, ClaimsPrincipal user, IChatsService chats, CancellationToken cancellationToken)
        => (await chats.DeleteAsync(user.FindSubjectId()!, chatId, cancellationToken))
            ? TypedResults.NoContent()
            : TypedResults.NotFound();

    /// <summary>POST /api/my/chats/{chatId}/messages/{messageId}/like — likes or dislikes a message.</summary>
    public static async Task<Results<NoContent, NotFound>> Like(Guid chatId, Guid messageId, LikeRequest request, ClaimsPrincipal user, IChatsService chats, CancellationToken cancellationToken)
        => (await chats.SetLikeAsync(user.FindSubjectId()!, chatId, messageId, request.Like, cancellationToken))
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
}
