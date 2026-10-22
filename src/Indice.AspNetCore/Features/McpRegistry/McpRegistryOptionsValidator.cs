using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Features.McpRegistry;

/// <summary>Validates the <see cref="McpRegistryOptions"/> against the MCP registry specification constraints.</summary>
internal sealed partial class McpRegistryOptionsValidator : IValidateOptions<McpRegistryOptions>
{
    [GeneratedRegex("^[a-zA-Z0-9.-]+/[a-zA-Z0-9._-]+$")]
    private static partial Regex ServerNameRegex();

    public ValidateOptionsResult Validate(string? name, McpRegistryOptions options) {
        var errors = new List<string>();
        if (options.DefaultPageSize < 1) {
            errors.Add($"{nameof(McpRegistryOptions.DefaultPageSize)} must be greater than zero.");
        }
        if (options.MaxPageSize < 1) {
            errors.Add($"{nameof(McpRegistryOptions.MaxPageSize)} must be greater than zero.");
        }
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < options.Servers.Count; i++) {
            var server = options.Servers[i];
            var prefix = $"{nameof(McpRegistryOptions.Servers)}[{i}]";
            if (string.IsNullOrWhiteSpace(server.Name) || !ServerNameRegex().IsMatch(server.Name)) {
                errors.Add($"{prefix}.{nameof(McpServerDetail.Name)} '{server.Name}' must be in reverse-DNS format containing exactly one forward slash (e.g. 'io.github.user/weather').");
            }
            if (string.IsNullOrWhiteSpace(server.Description)) {
                errors.Add($"{prefix}.{nameof(McpServerDetail.Description)} is required.");
            }
            if (string.IsNullOrWhiteSpace(server.Version)) {
                errors.Add($"{prefix}.{nameof(McpServerDetail.Version)} is required.");
            } else if (McpRegistryHandlers.LatestVersion.Equals(server.Version, StringComparison.OrdinalIgnoreCase)) {
                errors.Add($"{prefix}.{nameof(McpServerDetail.Version)} cannot be '{McpRegistryHandlers.LatestVersion}'.");
            } else if (!string.IsNullOrWhiteSpace(server.Name) && !keys.Add($"{server.Name}@{server.Version}")) {
                errors.Add($"{prefix} is a duplicate of server '{server.Name}' version '{server.Version}'.");
            }
            if (server.Remotes is not null) {
                for (var j = 0; j < server.Remotes.Count; j++) {
                    if (string.IsNullOrWhiteSpace(server.Remotes[j].Url)) {
                        errors.Add($"{prefix}.{nameof(McpServerDetail.Remotes)}[{j}].{nameof(McpRemote.Url)} is required.");
                    }
                }
            }
            if (server.Packages is not null) {
                for (var j = 0; j < server.Packages.Count; j++) {
                    var package = server.Packages[j];
                    if (string.IsNullOrWhiteSpace(package.RegistryType) || string.IsNullOrWhiteSpace(package.Identifier)) {
                        errors.Add($"{prefix}.{nameof(McpServerDetail.Packages)}[{j}] requires both {nameof(McpPackage.RegistryType)} and {nameof(McpPackage.Identifier)}.");
                    }
                }
            }
        }
        return errors.Count > 0 ? ValidateOptionsResult.Fail(errors) : ValidateOptionsResult.Success;
    }
}
