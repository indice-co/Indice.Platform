using Indice.Types;

namespace Indice.Features.Identity.SignInLogs.Models;

/// <summary>Represents a filter for an <see cref="SignInLogEntry"/> query.</summary>
public class SignInLogEntryFilter
{
    /// <summary>The time period to search for log data.</summary>
    public Period Period { get; }
    /// <summary>The unique identifier of the principal actor (i.e. user id, client id).</summary>
    public string PrincipalActorIdentifier { get; }
    /// <summary>The type of the principal actor (i.e. user, machine).</summary>
    public string PrincipalActorType { get; }
    /// <summary>The unique identifier of the resource.</summary>
    public string AuditedResourceIdentifier { get; }
    /// <summary>The type of the resource.</summary>
    public string AuditedResourceType { get; }
}
