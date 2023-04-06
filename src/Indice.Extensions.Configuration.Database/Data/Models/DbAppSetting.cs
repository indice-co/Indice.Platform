namespace Indice.Extensions.Configuration.Database.Data.Models;

/// <summary>Models application settings stored in the database.</summary>
public class DbAppSetting
{
    /// <summary>The key of application setting.</summary>
    public string Key { get; set; }
    /// <summary>The value of application setting.</summary>
    public string Value { get; set; }
}
