using Indice.Types;

namespace Indice.Features.Identity.SignInLogs.Models;

/// <summary>
/// Represents the data model for a sign-in log map, containing geographic sign-in locations and a legend for country
/// codes.
/// </summary>
/// <remarks>Use this class to provide data for visualizing sign-in activity by geographic location. The Items
/// property supplies the data points for the map, while the CountryLegend property enables display of user-friendly
/// country names alongside country codes.</remarks>
public class SignInLogMap
{
    /// <summary>
    /// A list of <see cref="SignInLogLocation"/> objects, each representing a geographic location 
    /// and the number of sign-in logs associated with that location. 
    /// This list is used to populate the sign-in log map with data points that indicate where sign-ins are occurring around the world. 
    /// Each entry in the list includes a country code, a geographic point (latitude and longitude), and a count of sign-ins from that location.
    /// </summary>
    public List<SignInLogLocation> Items { get; set; } = [];

    /// <summary>
    /// A dictionary that maps two-letter ISO country codes to their corresponding country names. This is used to provide a human-readable legend for the country codes in the sign-in log map. 
    /// </summary>
    public Dictionary<string, string> CountryLegend { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Represents the number of sign in logs per location.
/// </summary>
/// <param name="CountryCode">Two Letter ISO Country code</param>
/// <param name="Location">Geography Point</param>
/// <param name="Count">Number of signins from this location</param>
public record SignInLogLocation(string CountryCode, GeoPoint Location, int Count);
