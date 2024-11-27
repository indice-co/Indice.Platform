namespace Indice.Features.Identity.Core.Data.Models;

/// <summary>A user password hashed stored for password history validation purposes.</summary>
public class UserPassword
{
    /// <summary>Constructs a new instance of <see cref="UserPassword"/> with a new Guid Id.</summary>
    public UserPassword() {
        Id = Guid.NewGuid();
    }

    /// <summary>The primary key.</summary>
    public Guid Id { get; set; }
    /// <summary>The user id related.</summary>
    public string UserId { get; set; } = null!;
    /// <summary>Password hash.</summary>
    public string? PasswordHash { get; set; }
    /// <summary>The date this password was created.</summary>
    public DateTimeOffset DateCreated { get; set; }
}
