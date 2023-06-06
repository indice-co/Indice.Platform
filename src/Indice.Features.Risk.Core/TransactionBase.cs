using Indice.Types;

namespace Indice.Features.Risk.Core;

/// <summary>Models an transaction that occurred in the system.</summary>
public class TransactionBase
{
    /// <summary>The unique id of the transaction.</summary>
    public Guid Id { get; internal set; }
    /// <summary>An amount relative to the transaction.</summary>
    public decimal? Amount { get; set; }
    /// <summary>The coordinates accompanying the transaction.</summary>
    public GeoPoint? Coordinates { get; set; }
    /// <summary>The user IP address related to the transaction occurred.</summary>
    public string? IpAddress { get; set; }
}
