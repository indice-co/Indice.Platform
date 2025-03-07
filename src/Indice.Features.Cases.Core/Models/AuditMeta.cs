using System.Security.Claims;
using Indice.Security;

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
    public void Update(ClaimsPrincipal user, DateTimeOffset? now = null) {
        Populate(this, user, now);
    }

    /// <summary>Create a new instance from a <see cref="ClaimsPrincipal"/> object.</summary>
    /// <param name="user">The <see cref="ClaimsPrincipal"/>.</param>
    /// <param name="now">The timestamp</param>
    /// <returns></returns>
    public static AuditMeta Create(ClaimsPrincipal user, DateTimeOffset? now = null) {
        return Populate(null, user, now);
    }

    private static AuditMeta Populate(AuditMeta? meta, ClaimsPrincipal user, DateTimeOffset? now = null) {
        meta ??= new AuditMeta();

        /*
         * meta.Id logic:
         * When the ClaimsPrincipal has Subject, then there is an authorized user that access a case.
         * When the ClaimsPrincipal does not have Subject, we're creating a case through a proxy that has been  authorized via client-credentials.
         */

        var subject = user.FindFirstValue(BasicClaimTypes.Subject);
        meta.Id = string.IsNullOrWhiteSpace(subject)
            ? user.FindFirstValue(BasicClaimTypes.ClientId)
            : subject;
        meta.Email = string.IsNullOrWhiteSpace(subject)
            ? user.FindFirstValue(BasicClaimTypes.ClientId)
            : user.FindFirstValue(BasicClaimTypes.Email);
        meta.Name = string.IsNullOrWhiteSpace(subject)
            ? CasesCoreConstants.SystemUserName
            : $"{user.FindFirstValue(BasicClaimTypes.GivenName)} {user.FindFirstValue(BasicClaimTypes.FamilyName)}".Trim();
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