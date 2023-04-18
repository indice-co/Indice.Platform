using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;

namespace Indice.Features.Identity.Tests;

internal class UserStateProviderNoop : IUserStateProvider<User>
{
    public UserState CurrentState => UserState.LoggedIn;

    public void ChangeState(User user, UserAction action) { }

    public void ClearState() { }
}
