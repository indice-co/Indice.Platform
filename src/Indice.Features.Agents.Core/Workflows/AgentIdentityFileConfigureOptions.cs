using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows;

/// <summary>
/// Loads <see cref="AgentsOptions.Identity"/> from <c>{ContentRoot}/Prompts/AgentIdentity.json</c>, next to the
/// <c>AgentIdentity</c> prompt template, the same way prompt templates are discovered. The file's root is the identity
/// itself (<c>Name</c>, <c>Company</c>). When present it replaces any identity bound from the <c>Dex:Identity</c>
/// configuration section; when absent the options are left untouched.
/// </summary>
public sealed class AgentIdentityFileConfigureOptions : IConfigureOptions<AgentsOptions> {
    /// <summary>The identity values file name, looked up in the host's <c>Prompts</c> folder.</summary>
    public const string FileName = "AgentIdentity.json";

    private readonly IHostEnvironment _environment;

    /// <summary>Creates a new <see cref="AgentIdentityFileConfigureOptions"/>.</summary>
    public AgentIdentityFileConfigureOptions(IHostEnvironment environment) {
        _environment = environment;
    }

    /// <inheritdoc/>
    public void Configure(AgentsOptions options) {
        var path = Path.Join(_environment.ContentRootPath, "Prompts", FileName);
        if (!File.Exists(path)) {
            return;
        }
        // A fresh instance, so the file's Products replace (rather than append to) any configured identity.
        options.Identity = JsonSerializer.Deserialize<AgentsOptions.AgentIdentityOptions>(File.ReadAllText(path), _jsonOptions)
            ?? throw new InvalidOperationException($"Agent identity file '{path}' is empty.");
    }

    // As lenient as configuration JSON: case-insensitive names, comments and trailing commas allowed.
    private static readonly JsonSerializerOptions _jsonOptions = new() {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };
}
