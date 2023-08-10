using Indice.Features.Messages.Core.Services.Abstractions;

namespace Indice.Features.Messages.Core.Services;

/// <summary>Contains a method to resolve username in various environments.</summary>
public class UserNameAccessorAggregate
{
    private readonly IEnumerable<IUserNameAccessor> _userNameAccessors;

    /// <summary>Creates a new instance of <see cref="UserNameAccessorAggregate"/> class.</summary>
    /// <param name="userNameAccessors">The list of registered <see cref="IUserNameAccessor"/> implementations.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public UserNameAccessorAggregate(IEnumerable<IUserNameAccessor> userNameAccessors) {
        _userNameAccessors = userNameAccessors ?? throw new ArgumentNullException(nameof(userNameAccessors));
    }

    /// <summary>Resolves the username.</summary>
    public string Resolve() {
        string userName = null;
        foreach (var accessor in _userNameAccessors) {
            userName = accessor.Resolve();
            if (!string.IsNullOrWhiteSpace(userName)) {
                break;
            }
        }
        return userName;
    }
}
