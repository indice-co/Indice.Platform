using System.Security.Claims;
using Indice.Features.Cases.Core.Models;

namespace Indice.Features.Cases.Core.Services.Abstractions;

/// <summary>The case message service for sending <see cref="Message"/> to a case.</summary>
public interface ICaseMessageService
{
    ///  <summary>
    ///  Send a <see cref="Message"/> to case that will allow the case to do any or all of these:
    ///  <list type="number">
    ///  <item>Create checkpoint</item>
    ///  <item>Add attachment</item>
    ///  <item>Add comment</item>
    ///  <item>Reply to Comment with comment and/or attachments</item>
    ///  <item>Flag comment as private for the customer.</item>
    /// </list>
    /// </summary>
    ///  <param name="caseId">The Id of the case.</param>
    ///  <param name="user">The user that creates the message.</param>
    ///  <param name="message">The message to send.</param>
    ///  <returns></returns>
    Task<Guid?> Send(Guid caseId, ClaimsPrincipal user, Message message);
}

/// <summary>Placeholder to indicate MyCase feature.</summary>
public interface IMyCaseMessageService : ICaseMessageService
{

}

/// <summary>Placeholder to indicate Admin case feature.</summary>
public interface IAdminCaseMessageService : ICaseMessageService
{

}

/// <summary>
/// Extension methods on <see cref="ICaseMessageService"/>
/// </summary>
public static class ICaseMessageServiceExtensions
{

    /// <summary>Send a <see cref="Message"/> that is specialized to create a new comment with an exception message and a private comment.</summary>
    /// <param name="caseMessageService">The message service to extend</param>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="user">The user that creates the message.</param>
    /// <param name="exception">The exception to send.</param>
    /// <param name="message">The message to log.</param>
    /// <returns></returns>
    public static Task<Guid?> Send(this ICaseMessageService caseMessageService, Guid caseId, ClaimsPrincipal user, Exception exception, string? message = null) =>
        caseMessageService.Send(caseId, user, new Message {
            Comment = string.IsNullOrEmpty(message)
            ? $"Faulted with message: {exception.Message}"
            : $"Faulted with message: {message} and exception message: {exception.Message}",
            PrivateComment = true
        });
    
}