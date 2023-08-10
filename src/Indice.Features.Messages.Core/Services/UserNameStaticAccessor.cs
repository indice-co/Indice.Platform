using Indice.Features.Messages.Core.Services.Abstractions;

namespace Indice.Features.Messages.Core.Services;

/// <summary>An implementation of <see cref="IUserNameAccessor"/> that resolves the username using a provided static string.</summary>
public class UserNameStaticAccessor : IUserNameAccessor
{
    private readonly string _userName;

    /// <summary>Creates a new instance of <see cref="UserNameStaticAccessor"/>.</summary>
    /// <param name="userName">The username.</param>
    /// <exception cref="ArgumentNullException">Throws when <paramref name="userName"/> is null or empty.</exception>
    public UserNameStaticAccessor(string userName) {
        if (string.IsNullOrWhiteSpace(userName)) {
            throw new ArgumentNullException(nameof(userName));
        }
        _userName = userName;
    }

    /// <inheritdoc />
    public int Priority => 2;

    /// <inheritdoc />
    public string Resolve() => _userName;
}
