using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Data;
using Indice.Features.Identity.SignInLogs.Models;
using Indice.Types;

namespace Indice.Features.Identity.Server.ImpossibleTravel.Services;

internal class ImpossibleTravelDetector<TUser> where TUser : User
{
    private readonly ISignInLogStore? _signInLogStore;

    public ImpossibleTravelDetector(ISignInLogStore? signInLogStore = null) {
        _signInLogStore = signInLogStore;
    }

    public async Task<bool> IsImpossibleTravelLogin() {
        if (_signInLogStore == null) {
            return false;
        }
        var previousLogin = (await _signInLogStore.ListAsync(
            new ListOptions {
                Page = 1,
                Size = 1,
                Sort = $"{nameof(DbSignInLogEntry.CreatedAt)}-"
            },
            new SignInLogEntryFilter {
                From = DateTimeOffset.UtcNow,
                SignInType = SignInType.Interactive,
                Subject = "",
                To = DateTimeOffset.UtcNow
            }
        ))
        .Items
        .FirstOrDefault();
        if (previousLogin is null) {
            return false;
        }
        var previousLoginCoordinates = previousLogin.Coordinates;
        return true;
    }
}
