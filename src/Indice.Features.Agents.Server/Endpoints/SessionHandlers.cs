using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Agents.Server.Endpoints;

internal class SessionHandlers
{
    public static async Task<Results<Ok<SessionResponse>, ProblemHttpResult>> CreateSession(HttpContext context, SessionRequest request, CancellationToken cancellationToken) {
        //var sessionService = context.RequestServices.GetRequiredService<ISessionService>();
        //var session = await sessionService.CreateSessionAsync(request, cancellationToken);
        //return TypedResults.Ok(session);
        return TypedResults.Problem("Session service is not implemented.", statusCode: StatusCodes.Status501NotImplemented);
    }
}

/// <summary>
/// Represents a request to create a new session.
/// </summary>
/// <param name="ExternalRefId"> An optional external reference ID associated with the session.</param>
/// <param name="Referrer"> An optional referrer URL indicating the source of the session request.</param>
public record SessionRequest(string? ExternalRefId, string? Referrer);
/// <summary>
/// Represents the response returned after creating a new session.
/// </summary>
/// <param name="SessionId">The unique identifier of the created session.</param>
/// <param name="ExpiresAt">The expiration date and time of the session.</param>
public record SessionResponse(string SessionId, DateTime ExpiresAt);
