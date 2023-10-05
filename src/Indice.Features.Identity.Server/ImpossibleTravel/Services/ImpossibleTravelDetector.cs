using System.Net;
using Indice.AspNetCore.Extensions;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.ImpossibleTravel;
using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Data;
using Indice.Features.Identity.SignInLogs.Models;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.Server.ImpossibleTravel.Services;

/// <summary>A service that detects whether a login attempt is made from an impossible location.</summary>
/// <typeparam name="TUser"></typeparam>
public class ImpossibleTravelDetector<TUser> : IImpossibleTravelDetector<TUser> where TUser : User
{
    private readonly ISignInLogStore? _signInLogStore;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary></summary>
    /// <param name="httpContextAccessor">Provides access to the current <see cref="HttpContext"/>, if one is available.</param>
    /// <param name="options">Configuration options for impossible travel detector feature.</param>
    /// <param name="signInLogStore">A service that contains operations used to persist the data of a user's sign in event.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public ImpossibleTravelDetector(
        IHttpContextAccessor httpContextAccessor,
        IOptions<ImpossibleTravelDetectorOptions> options,
        ISignInLogStore? signInLogStore = null) {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        Options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _signInLogStore = signInLogStore;
    }

    /// <inheritdoc />
    public ImpossibleTravelDetectorOptions Options { get; init; }

    /// <inheritdoc />
    public async Task<bool> IsImpossibleTravelLogin(TUser user) {
        if (_signInLogStore is null || _httpContextAccessor.HttpContext is null || user is null) {
            return false;
        }
        var previousLogins = (await _signInLogStore.ListAsync(
            new ListOptions {
                Page = 1,
                Size = 2,
                Sort = $"{nameof(DbSignInLogEntry.CreatedAt)}-"
            },
            new SignInLogEntryFilter {
                SignInType = SignInType.Interactive,
                Subject = user.Id,
                To = DateTimeOffset.UtcNow
            }
        ))
        .Items;
        if (previousLogins is null || !previousLogins.Any()) {
            return false;
        }
        var ipAddress = _httpContextAccessor.HttpContext.GetClientIpAddress();
        if (ipAddress is null) {
            return false;
        }
        var result = false;
        foreach (var previousLogin in previousLogins) {
            var currentLoginCoordinates = ipAddress.GetLocationMetadata()?.Coordinates;
            var previousLoginCoordinates = previousLogin.Coordinates;
            if (currentLoginCoordinates is null || previousLoginCoordinates is null) {
                continue;
            }
            var distanceBetweenLogins = currentLoginCoordinates.Distance(previousLoginCoordinates);
            var travelSpeed = distanceBetweenLogins / (DateTimeOffset.UtcNow - previousLogin.CreatedAt).TotalHours;
            if (travelSpeed > Options.AcceptableSpeed) {
                result = true;
                break;
            }
        }
        return result;
    }
}