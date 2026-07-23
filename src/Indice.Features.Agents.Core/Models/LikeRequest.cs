namespace Indice.Features.Agents.Core.Models;

/// <summary>Represents a request to like or dislike a message.</summary>
public class LikeRequest
{
    /// <summary>Indicates whether the message is liked or disliked.</summary>
    public bool? Like { get; set; }
}
