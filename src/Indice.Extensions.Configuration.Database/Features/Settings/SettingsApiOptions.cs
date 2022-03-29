using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.AspNetCore.Features.Settings
{
    /// <summary>
    /// Options used to configure the Settings API feature.
    /// </summary>
    public class SettingsApiOptions
    {
        internal IServiceCollection Services { get; set; }
        /// <summary>
        /// Configuration <see cref="Action"/> for internal <see cref="DbContext"/>. 
        /// If not provided the underlying store defaults to SQL Server expecting the setting <i>ConnectionStrings:SettingsDbConnection</i> to be present.
        /// </summary>
        public Action<DbContextOptionsBuilder> ConfigureDbContext { get; set; }
        /// <summary>
        /// Specifies a prefix for the API endpoints. Defaults to <i>api</i>.
        /// </summary>
        public string ApiPrefix { get; set; } = "api";
        /// <summary>
        /// The default scope name to be used for Settings API. Defaults to <see cref="SettingsApi.Scope"/>.
        /// </summary>
        public string RequiredScope { get; set; } = SettingsApi.Scope;
        /// <summary>
        /// list of Authentication schemes. If left empty the default scheme will be used
        /// </summary>
        public ICollection<string> AuthenticationSchemes { get; set; } = new Collection<string>();
    }
}
