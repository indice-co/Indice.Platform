using Indice.Features.Media.AspNetCore.Models;

namespace Indice.Features.Media.AspNetCore.Services;
/// <summary>The service used to handle media setting related operations.</summary>
public interface IMediaSettingService
{
    /// <summary>Retrieves an existing setting's value.</summary>
    /// <param name="key">The setting's key.</param>
    Task<MediaSetting?> GetSetting(string key);
    /// <summary>Lists all the Media Settings.</summary>
    Task<List<MediaSetting>> ListSettings();
    /// <summary>Updates an existing setting.</summary>
    /// <param name="key">The setting's key.</param>
    /// <param name="value">The setting's value.</param>
    Task UpdateSetting(string key, string value);
}
