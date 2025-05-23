namespace Indice.Features.Identity.Server.Manager.Models;

/// <summary>Models a claim.</summary>
public class ClaimInfo : BasicClaimInfo
{
    /// <summary>The id of the user claim entry.</summary>
    public int Id { get; set; }
    /// <summary>The display name of the claim.</summary>
    public string? DisplayName { get; set; }
}

/// <summary>Models a claim.</summary>
public class BasicClaimInfo
{
    /// <summary>The type of the claim.</summary>
    public string? Type { get; set; }
    /// <summary>The value of the claim.</summary>
    public string? Value { get; set; }

    /// <summary>
    /// Compares two <see cref="ClaimInfo"/> objects for equality based on their type and value.
    /// </summary>
    public static readonly IEqualityComparer<BasicClaimInfo> DefaultComparer = new ClaimInfoComparer<BasicClaimInfo>();

    /// <summary>Compares two <see cref="ClaimInfo"/> objects for equality.</summary>
    public class ClaimInfoComparer<TClaimInfo> : IEqualityComparer<TClaimInfo> where TClaimInfo : BasicClaimInfo
    {
        /// <summary>Compares two <see cref="ClaimInfo"/> objects for equality.</summary>
        public bool Equals(TClaimInfo? x, TClaimInfo? y) {
            return x?.Type == y?.Type && x?.Value == y?.Value;
        }
        /// <summary>Gets the hash code for a <see cref="BasicClaimInfo"/> object.</summary>
        public int GetHashCode(TClaimInfo obj) {
            return HashCode.Combine(obj.Type, obj.Value);
        }
    }
}
