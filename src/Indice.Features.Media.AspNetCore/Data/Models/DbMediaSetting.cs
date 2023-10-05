namespace Indice.Features.Media.AspNetCore.Data.Models;
/// <summary>The MediaSettings entity.</summary>
public class DbMediaSetting: DbAuditableEntity
{
    /// <summary>The Key of the Setting</summary>
    public required string Key { get; set; }
    /// <summary>The Setting's Value</summary>
    public string? Value { get; set; }
}
