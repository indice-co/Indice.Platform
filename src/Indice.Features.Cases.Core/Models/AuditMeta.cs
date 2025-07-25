using System.Security.Claims;

namespace Indice.Features.Cases.Core.Models;

/// <summary>Audit metadata related with the user principal that "did" the action.</summary>
public class AuditMeta : ICloneable
{
    /// <summary>The Id of the user.</summary>
    public string? Id { get; set; }

    /// <summary>The name of the user.</summary>
    public string? Name { get; set; }

    /// <summary>The email of the user.</summary>
    public string? Email { get; set; }

    /// <summary>The timestamp the audit happened.</summary>
    public DateTimeOffset? When { get; set; } = DateTimeOffset.Now;

    /// <summary>Clear the data of the instance.</summary>
    public void Clear() {
        Id = null;
        Name = null;
        Email = null;
        When = null;
    }

    /// <summary>Update the current instance with a new principal.</summary>
    /// <param name="user">The new principal to update the instance.</param>
    /// <param name="now">The timestamp.</param>
    public void Update(UserActor user, DateTimeOffset? now = null) {
        Populate(this, user, now);
    }

    /// <summary>Create a new instance from a <see cref="ClaimsPrincipal"/> object.</summary>
    /// <param name="user">The <see cref="ClaimsPrincipal"/>.</param>
    /// <param name="now">The timestamp</param>
    /// <returns></returns>
    public static AuditMeta Create(UserActor user, DateTimeOffset? now = null) {
        return Populate(null, user, now);
    }

    private static AuditMeta Populate(AuditMeta? meta, UserActor user, DateTimeOffset? now = null) {
        meta ??= new AuditMeta();

        

        meta.Id = user.Id;
        meta.Email = user.Email;
        meta.Name = user.Name;
        meta.When = now ?? DateTimeOffset.UtcNow;
        return meta;
    }

    /// <inheritdoc/>
    object ICloneable.Clone() => this.Clone();

    /// <summary>Creates a new object that is a copy of the current instance.</summary>
    /// <returns>A new object that is a copy of this instance.</returns>
    public AuditMeta Clone() => new () {
        Id = Id,
        Name= Name,
        Email = Email,
        When = When,
    };
}