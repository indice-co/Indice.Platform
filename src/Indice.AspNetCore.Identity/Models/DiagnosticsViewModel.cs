using System.Text;
using System.Text.Json;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;

namespace Indice.AspNetCore.Identity.Models;

/// <summary>Diagnostics view model</summary>
public class DiagnosticsViewModel
{
    /// <summary>constructs the viewmodel</summary>
    /// <param name="result"></param>
    public DiagnosticsViewModel(AuthenticateResult result) {
        AuthenticateResult = result;

        if (result.Properties.Items.ContainsKey("client_list")) {
            var encoded = result.Properties.Items["client_list"];
            var bytes = Base64Url.Decode(encoded);
            var value = Encoding.UTF8.GetString(bytes);
            Clients = JsonSerializer.Deserialize<List<string>>(value);
        }
    }

    /// <summary>The authenticate result to debug</summary>
    public AuthenticateResult AuthenticateResult { get; }

    /// <summary>List of clients.</summary>
    public List<string> Clients { get; } = [];
}
