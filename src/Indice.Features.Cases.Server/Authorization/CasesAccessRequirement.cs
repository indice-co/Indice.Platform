using Microsoft.AspNetCore.Authorization;

namespace Indice.Features.Cases.Server.Authorization;

/// <summary>
/// Represents the level of access on the Case management system
/// </summary>
public enum CasesAccessLevel
{
    /// <summary>Require read access.</summary>
    Read = 0,
    /// <summary>
    /// Require access to manage cases.
    /// </summary>
    Manage = 1,
    /// Require access to administer cases.
    Administer = 2
}

/// <summary>This authorization requirement specifies that an endpoint must be accessible only to Messaging tool managers.</summary>
public class CasesSystemAccessRequirement : IAuthorizationRequirement
{
    /// <summary>The policy name corresponding to this requirement.</summary>
    public const string PolicyName = CaseServerConstants.Policies.BeCasesManager;

    /// <summary>Creates a new instance of <see cref="CasesSystemAccessRequirement"/>.</summary>
    public CasesSystemAccessRequirement(CasesAccessLevel minimumAccessLevel = CasesAccessLevel.Read) {
        MinimumAccessLevel = minimumAccessLevel;
    }

    /// <summary>The minimum access level needed to access the protected resources</summary>
    public CasesAccessLevel MinimumAccessLevel { get; }
    /// <inheritdoc/>
    public override string ToString() => $"Requires Cases {MinimumAccessLevel} Access.";
}

/// <summary>This authorization requirement specifies that an endpoint must be accessible only by Admins, delegated user clients and user with access to the case.</summary>
public class CasesRecordsAccessLevelRequirement : IAuthorizationRequirement
{
    /// <summary>Creates a new instance of <see cref="CasesRecordsAccessLevelRequirement"/>.</summary>
    /// <param name="minimumAccessLevel"></param>
    public CasesRecordsAccessLevelRequirement(CasesAccessLevel minimumAccessLevel = CasesAccessLevel.Read) {
        MinimumAccessLevel = minimumAccessLevel;
    }

    /// <summary>The minimum access level needed to access the protected resources</summary>
    public CasesAccessLevel MinimumAccessLevel { get; }
    /// <inheritdoc/>
    public override string ToString() => $"Requires Cases {MinimumAccessLevel} Access.";
}

/// <summary>This authorization requirement specifies that an endpoint must be accessible only by the owner of the Case.</summary>
public class CasesOwnerAccessRequirement : IAuthorizationRequirement
{
    /// <summary>Creates a new instance of <see cref="CasesOwnerAccessRequirement"/>.</summary>
    public CasesOwnerAccessRequirement() { }
}