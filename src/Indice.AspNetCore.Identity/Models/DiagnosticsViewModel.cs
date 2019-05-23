using System.Collections.Generic;
using System.Text;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Newtonsoft.Json;

namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>
    /// Diagnostics view model
    /// </summary>
    public class DiagnosticsViewModel
    {
        /// <summary>
        /// constructs the viewmodel
        /// </summary>
        /// <param name="result"></param>
        public DiagnosticsViewModel(AuthenticateResult result) {
            AuthenticateResult = result;

            if (result.Properties.Items.ContainsKey("client_list")) {
                var encoded = result.Properties.Items["client_list"];
                var bytes = Base64Url.Decode(encoded);
                var value = Encoding.UTF8.GetString(bytes);
                Clients = JsonConvert.DeserializeObject<string[]>(value);
            }
        }

        /// <summary>
        /// The authenticate result to debug
        /// </summary>
        public AuthenticateResult AuthenticateResult { get; }

        /// <summary>
        /// List of clients.
        /// </summary>
        public IEnumerable<string> Clients { get; } = new List<string>();
    }
}
