using Indice.Features.Identity.Core.Data.Models;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Identity.Core.ImpossibleTravel;

/// <summary>A service that detects whether a login attempt is made from an impossible location.</summary>
/// <typeparam name="TUser"></typeparam>
public interface IImpossibleTravelDetector<TUser> where TUser : User
{
    /// <summary>Specifies the flow to follow when impossible travel is detected for the current user.</summary>
    public ImpossibleTravelFlowType FlowType { get; }
    /// <summary>Detects whether a login attempt is made from an impossible location.</summary>
    /// <param name="httpContext">Encapsulates all HTTP-specific information about an individual HTTP request.</param>
    /// <param name="user">The current user.</param>
    Task<bool> IsImpossibleTravelLogin(HttpContext? httpContext, TUser user);
}