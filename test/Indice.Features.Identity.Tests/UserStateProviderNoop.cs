using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;

namespace Indice.Features.Identity.Tests;

internal class UserStateProviderNoop : IUserStateProvider<User>
{
    public UserState CurrentState => UserState.LoggedOut;

    public Task ChangeStateAsync(User user, UserAction action) => Task.CompletedTask;

    public void ClearState() { }
}
