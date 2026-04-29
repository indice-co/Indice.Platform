using Indice.Types;

namespace Indice.Features.Identity.SignInLogs.Models;

/// <summary>
/// Represents the data model for a sign-in log map, containing geographic sign-in locations and a legend for country
/// codes.
/// </summary>
/// <remarks>Use this class to provide data for visualizing sign-in activity by geographic location. The Items
/// property supplies the data points for the map, while the CountryLegend property enables display of user-friendly
/// country names alongside country codes.</remarks>
public class SignInLocationSet : ResultSet<SignInLogLocation>
{
    /// <summary>
    /// Initializes a new instance of the SignInLocationSet class with the specified collection of sign-in log locations and
    /// the total count of items.
    /// </summary>
    /// <param name="items">The collection of SignInLogLocation objects to include in the map. Cannot be null.</param>
    /// <param name="totalCount">The total number of sign-in log entries represented by the map. Must be greater than or equal to zero.</param>
    public SignInLocationSet(IEnumerable<SignInLogLocation> items, int totalCount) : base(items, totalCount) {
            
    }
    /// <summary>
    /// A dictionary that maps two-letter ISO country codes to their corresponding country names. This is used to provide a human-readable legend for the country codes in the sign-in log map. 
    /// </summary>
    public Dictionary<string, string> CountryLegend { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Represents the number of sign in logs per location.
/// </summary>
/// <param name="CountryCode">Two Letter ISO Country code</param>
/// <param name="DisplayName">Location Name</param>
/// <param name="Location">Geography Point</param>
/// <param name="Count">Number of signins from this location</param>
public record SignInLogLocation(string CountryCode, string DisplayName, GeoPoint Location, int Count);
