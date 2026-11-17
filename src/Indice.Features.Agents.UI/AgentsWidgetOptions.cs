namespace Indice.Features.Agents.UI;

/// <summary>Options for the embeddable chat widget (<c>widget.js</c> and the compact <c>/embed</c> route it frames).</summary>
public class AgentsWidgetOptions
{
    /// <summary>
    /// The origins (<c>scheme://host[:port]</c>, no path and no wildcards) of the sites allowed to embed the widget.
    /// They are sent as <c>Content-Security-Policy: frame-ancestors</c> on the <c>/embed</c> page and are the only parent
    /// origins the embedded page talks to over <c>postMessage</c>. The UI's own origin is always allowed. Defaults to none.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = [];

    /// <summary>The validated, normalized <see cref="AllowedOrigins"/>, resolved once by <c>UseAgentsUI</c>.</summary>
    internal string[] Origins { get; set; } = [];

    /// <summary>
    /// Validates <see cref="AllowedOrigins"/> and returns them normalized the way browsers report an origin (lower-case
    /// scheme and host, default port omitted), without duplicates.
    /// </summary>
    /// <exception cref="InvalidOperationException">An entry is not an exact <c>http</c>/<c>https</c> origin.</exception>
    internal string[] GetNormalizedOrigins() {
        var origins = new List<string>();
        foreach (var entry in AllowedOrigins ?? []) {
            var value = entry?.Trim();
            if (string.IsNullOrEmpty(value)) {
                continue;
            }
            var valid = !value.Contains('*')
                && Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
                && string.IsNullOrEmpty(uri.UserInfo)
                && uri.AbsolutePath == "/"
                && string.IsNullOrEmpty(uri.Query)
                && string.IsNullOrEmpty(uri.Fragment);
            if (!valid) {
                throw new InvalidOperationException($"Widget allowed origin '{entry}' is not a valid origin. Use an exact 'https://host[:port]' value, without a path or wildcards.");
            }
            var origin = new Uri(value).GetLeftPart(UriPartial.Authority);
            if (!origins.Contains(origin, StringComparer.Ordinal)) {
                origins.Add(origin);
            }
        }
        return [.. origins];
    }
}
