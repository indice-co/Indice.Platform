using Indice.AspNetCore.Extensions;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.ImpossibleTravel;
using Indice.Features.ActivityLogs.Abstractions;
using Indice.Features.ActivityLogs.Data;
using Indice.Features.ActivityLogs.Models;
using Indice.Features.GeoIP;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Indice.Features.ActivityLogs.ImpossibleTravel;

/// <summary>A service that detects whether a login attempt is made from an impossible location.</summary>
/// <typeparam name="TUser"></typeparam>
public class ImpossibleTravelDetector<TUser> : IImpossibleTravelDetector<TUser> where TUser : User
{
    private readonly IPAddressLocator? _ipAddressLocator;
    private readonly IActivityLogStore? _ActivityLogStore;
    private readonly ActivityLogOptions _ActivityLogOptions;

    /// <summary></summary>
    /// <param name="options">Configuration options for impossible travel detector feature.</param>
    /// <param name="ipAddressLocator"></param>
    /// <param name="ActivityLogStore">A service that contains operations used to persist the data of a user's activity event.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public ImpossibleTravelDetector(
        IOptions<ActivityLogOptions> options,
        IPAddressLocator? ipAddressLocator = null,
        IActivityLogStore? ActivityLogStore = null) {
        _ActivityLogOptions = options.Value ?? throw new ArgumentNullException(nameof(options));
        _ipAddressLocator = ipAddressLocator;
        _ActivityLogStore = ActivityLogStore;
    }

    /// <inheritdoc />
    public ImpossibleTravelFlowType FlowType => _ActivityLogOptions.ImpossibleTravel.FlowType;

    /// <inheritdoc />
    public async Task<bool> IsImpossibleTravelLogin(HttpContext? httpContext, TUser? user) {
        if (_ipAddressLocator is null || _ActivityLogStore is null || httpContext is null || user is null) {
            return false;
        }
        var previousLogin = (await _ActivityLogStore.ListAsync(
            new ListOptions {
                Page = 1,
                Size = 1,
                Sort = $"{nameof(DbActivityLogEntry.CreatedAt)}-"
            },
            new ActivityLogEntryFilter {
                ActivityType = ActivityType.Interactive,
                Subject = user.Id,
                To = DateTimeOffset.UtcNow,
                From = DateTimeOffset.UtcNow.AddDays(-_ActivityLogOptions.ImpossibleTravel.LookBackPeriodInDays),
                ActionName = "User Login Success"
            }
        ))
        .Items
        .FirstOrDefault();
        if (previousLogin is null) {
            return false;
        }
        var ipAddress = httpContext.GetClientIpAddress();
        if (ipAddress is null) {
            return false;
        }
        var currentLoginCoordinates = _ipAddressLocator.GetLocationMetadata(ipAddress)?.Coordinates;
        var previousLoginCoordinates = previousLogin.Coordinates;
        if (currentLoginCoordinates is null || previousLoginCoordinates is null) {
            return false;
        }
        var travelSpeed = currentLoginCoordinates.TravelSpeed(previousLoginCoordinates, previousLogin.CreatedAt, DateTimeOffset.UtcNow);
        return travelSpeed > _ActivityLogOptions.ImpossibleTravel.AcceptableSpeed;
    }
}
