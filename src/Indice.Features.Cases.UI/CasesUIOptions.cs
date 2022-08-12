using Indice.AspNetCore.EmbeddedUI;

namespace Indice.Features.Cases.UI
{
    /// <summary>
    /// Options for configuring <see cref="SpaUIMiddleware{TOptions}"/> middleware.
    /// </summary>
    public class CasesUIOptions : SpaUIOptions
    {
        /// <summary>
        /// Creates a new instance <see cref="CampaignUIOptions"/>.
        /// </summary>
        public CasesUIOptions() {
            ConfigureIndexParameters = (args) => { };
        }
    }
}
