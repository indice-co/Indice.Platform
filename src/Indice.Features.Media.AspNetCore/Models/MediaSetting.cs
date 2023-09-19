namespace Indice.Features.Media.AspNetCore.Models;
/// <summary>The MediaSetting entity.</summary>
public class MediaSetting
{
    /// <summary>The Key of the Setting</summary>
    public required string Key { get; set; }
    /// <summary>The Setting's Value</summary>
    public string? Value { get; set; }
    /// <summary>A short description about the Setting's usage.</summary>
    public string? Description { get; set; }
    /// <summary>Specifies the principal that created the entity.</summary>
    public string? CreatedBy { get; set; }
    /// <summary>Specifies when an entity was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Specifies the principal that update the entity.</summary>
    public string? UpdatedBy { get; set; }
    /// <summary>Specifies when an entity was updated.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    internal static readonly MediaSetting CDN = new() {
        Key = $"Media {nameof(CDN)}",
        Value = string.Empty,
        Description = "The full URL to the CDN used to serve uploaded media. Leave blank to use default storage URL."
    };

    /// <summary>Retrieves all MediaSettings</summary>
    /// <returns>A list of MediaSetting</returns>
    public static List<MediaSetting> GetAll() {
        return new List<MediaSetting>() {
            CDN
        };
    }
}
