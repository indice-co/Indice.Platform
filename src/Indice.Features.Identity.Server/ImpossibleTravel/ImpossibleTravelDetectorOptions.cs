namespace Indice.Features.Identity.Server.ImpossibleTravel;

/// <summary>Configuration options for impossible travel detector feature.</summary>
public class ImpossibleTravelDetectorOptions
{
    /// <summary>The speed (km/h) used to compare the travel speed between two login attempts.</summary>
    public double AcceptableSpeed { get; set; } = 80d;
}
