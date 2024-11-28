using System.Security.Claims;
using Indice.Features.Cases.Core.Models;

namespace Indice.Features.Cases.Core.Interfaces;

/// <summary>The case message service for sending <see cref="Message"/> to a case.</summary>
public interface ICaseMessageService
{
    ///  <summary>
    ///  Send a <see cref="Message"/> to case that will allow the case to do any or all of these:
    ///  1) Create checkpoint,
    ///  2) Add attachment,
    ///  3) Add comment,
    ///  4) Reply to Comment with comment and/or attachments,
    ///  5) Flag comment as private for the customer.
    /// </summary>
    ///  <param name="caseId">The Id of the case.</param>
    ///  <param name="user">The user that creates the message.</param>
    ///  <param name="message">The message to send.</param>
    ///  <returns></returns>
    Task<Guid?> Send(Guid caseId, ClaimsPrincipal user, Message message);

    /// <summary>Send a <see cref="Message"/> that is specialized to create a new comment with an exception message and a private comment.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="user">The user that creates the message.</param>
    /// <param name="exception">The exception to send.</param>
    /// <param name="message">The message to log.</param>
    /// <returns></returns>
    Task Send(Guid caseId, ClaimsPrincipal user, Exception exception, string? message = null);
}

/// <summary>Placeholder to indicate MyCase feature.</summary>
public interface IMyCaseMessageService : ICaseMessageService
{

}

/// <summary>Placeholder to indicate Admin case feature.</summary>
public interface IAdminCaseMessageService : ICaseMessageService
{

}