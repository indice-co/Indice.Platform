namespace Indice.Features.Identity.Core.Data.Models;

/// <summary><see cref="PasswordExpirationPolicy"/> enumeration defines all available password expiration presets. The value is measured in days. If cast to integer will give you a number in days to add to the current <seealso cref="DateTime"/>.</summary>
public enum PasswordExpirationPolicy
{
    /// <summary>The password never expires.</summary>
    Never = -1,
    /// <summary>The password expires every month.</summary>
    Monthly = 30,
    /// <summary>The password expires every three months.</summary>
    Quarterly = 3 * Monthly,
    /// <summary>The password expires every six months.</summary>
    Semesterly = 6 * Monthly,
    /// <summary>The password expires every year.</summary>
    Annually = 365,
    /// <summary>The password expires every two years.</summary>
    Biannually = 2 * Annually
}
