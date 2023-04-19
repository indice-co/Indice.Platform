using Indice.Features.Identity.Core.Data.Models;
using Indice.Types;

namespace Indice.Features.Identity.SignInLogs.Models;

/// <summary>A model representing a user's sign in log entry.</summary>
public class SignInLogEntry
{
    /// <summary>Creates a new instance of <see cref="SignInLogEntry"/> class, given the <paramref name="id"/> and <paramref name="createdAt"/> values.</summary>
    /// <param name="id">The unique id of the user sign in log entry.</param>
    /// <param name="createdAt">A timestamp that indicates when the user sign in log entry occurred.</param>
    public SignInLogEntry(Guid id, DateTimeOffset createdAt) {
        Id = id;
        CreatedAt = createdAt;
    }

    /// <summary>Creates a new instance of <see cref="SignInLogEntry"/> class.</summary>
    public SignInLogEntry() : this(Guid.NewGuid(), DateTimeOffset.UtcNow) { }

    internal User User { get; set; }
    /// <summary>The unique id of the user's sign in log entry.</summary>
    public Guid Id { get; set; }
    /// <summary>A timestamp that indicates when the user's sign in log entry occurred.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>The name of the action.</summary>
    public string ActionName { get; set; }
    /// <summary>The unique identifier of the application.</summary>
    public string ApplicationId { get; set; }
    /// <summary>The display name of the application.</summary>
    public string ApplicationName { get; set; }
    /// <summary>The unique identifier of the subject.</summary>
    public string SubjectId { get; set; }
    /// <summary>The display name of the subject.</summary>
    public string SubjectName { get; set; }
    /// <summary>The unique identifier of the resource.</summary>
    public string ResourceId { get; set; }
    /// <summary>The name of the resource.</summary>
    public string ResourceType { get; set; }
    /// <summary>A friendly text describing the log entry.</summary>
    public string Description { get; set; }
    /// <summary>Indicates whether the operation that caused the user's sign in log entry was successful or not.</summary>
    public bool Succeeded { get; set; }
    /// <summary>The IP address of the client.</summary>
    public string IpAddress { get; set; }
    /// <summary>The unique identifier of the current request.</summary>
    public string RequestId { get; set; }
    /// <summary>The estimated client location based on the <see cref="IpAddress"/>.</summary>
    public string Location { get; set; }
    /// <summary>User's session id.</summary>
    public string SessionId { get; set; }
    /// <summary>Describes the user sign in type in terms of user presence.</summary>
    public SignInType? SignInType { get; set; }
    /// <summary>Indicates whether the specified log entry is marked for review.</summary>
    public bool Review { get; set; }
    /// <summary>Two letter ISO code for the country.</summary>
    public string CountryIsoCode { get; set; }
    /// <summary>The device id.</summary>
    public string DeviceId { get; set; }
    /// <summary>The grant type used for the login.</summary>
    public string GrantType { get; set; }
    /// <summary>The approximate location of the operation.</summary>
    public GeoPoint Coordinates { get; set; }
    /// <summary>Additional information about the user's sign in log entry.</summary>
    public SignInLogEntryExtraData ExtraData { get; set; }
}
