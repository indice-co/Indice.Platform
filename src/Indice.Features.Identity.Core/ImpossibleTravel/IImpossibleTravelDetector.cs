using Indice.Features.Identity.Core.Data.Models;

namespace Indice.Features.Identity.Core.ImpossibleTravel;

/// <summary>A service that detects whether a login attempt is made from an impossible location.</summary>
/// <typeparam name="TUser"></typeparam>
public interface IImpossibleTravelDetector<TUser> where TUser : User
{
    /// <summary>Configuration options for impossible travel detector feature.</summary>
    public ImpossibleTravelDetectorOptions Options { get; init; }
    /// <summary>Detects whether a login attempt is made from an impossible location.</summary>
    /// <param name="user">The current user.</param>
    Task<bool> IsImpossibleTravelLogin(TUser user);
}
