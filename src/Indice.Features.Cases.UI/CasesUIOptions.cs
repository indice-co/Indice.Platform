using Indice.AspNetCore.EmbeddedUI;

namespace Indice.Features.Cases.UI
{
    /// <summary>
    /// Options for configuring <see cref="SpaUIMiddleware{TOptions}"/> middleware.
    /// </summary>
    public class CasesUIOptions : SpaUIOptions
    {
        /// <summary>
        /// The case management api url.
        /// </summary>
        public string ApiUrl { get; set; }

        /// <summary>
        /// Creates a new instance <see cref="CasesUIOptions"/>.
        /// </summary>
        public CasesUIOptions() {
            ConfigureIndexParameters = (args) => {
                args[nameof(ApiUrl)] = ApiUrl;
            };
        }
    }
}
