using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Features.McpRegistry;

internal static class McpRegistryHandlers
{
    internal const string LatestVersion = "latest";

    internal static Results<Ok<McpServerList>, BadRequest<McpRegistryError>> ListServers(
        IOptionsMonitor<McpRegistryOptions> options,
        [FromQuery] string? cursor,
        [FromQuery] int? limit,
        [FromQuery] string? search,
        [FromQuery(Name = "updated_since")] DateTimeOffset? updatedSince,
        [FromQuery] string? version,
        [FromQuery(Name = "include_deleted")] bool? includeDeleted) {
        var registry = options.CurrentValue;
        if (!TryDecodeCursor(cursor, out var offset)) {
            return TypedResults.BadRequest(new McpRegistryError("Invalid cursor"));
        }
        var latest = GetLatestVersions(registry.Servers);
        IEnumerable<McpServerEntry> query = registry.Servers;
        // Per spec, deleted entries are always included when 'updated_since' is provided.
        if (includeDeleted != true && !updatedSince.HasValue) {
            query = query.Where(x => x.Status != McpServerStatus.Deleted);
        }
        if (!string.IsNullOrWhiteSpace(search)) {
            query = query.Where(x => x.Name.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase));
        }
        if (updatedSince.HasValue) {
            query = query.Where(x => GetLastModified(x) >= updatedSince.Value);
        }
        if (!string.IsNullOrWhiteSpace(version)) {
            query = LatestVersion.Equals(version, StringComparison.OrdinalIgnoreCase)
                ? query.Where(latest.Contains)
                : query.Where(x => string.Equals(x.Version, version, StringComparison.OrdinalIgnoreCase));
        }
        var items = query.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                         .ThenByDescending(x => x, McpServerVersionComparer.Instance)
                         .ToList();
        var pageSize = Math.Clamp(limit ?? registry.DefaultPageSize, 1, Math.Max(1, registry.MaxPageSize));
        var page = items.Skip(offset).Take(pageSize).Select(x => ToResponse(x, latest.Contains(x))).ToList();
        var nextOffset = offset + page.Count;
        return TypedResults.Ok(new McpServerList {
            Servers = page,
            Metadata = new McpServerListMetadata {
                Count = page.Count,
                NextCursor = page.Count > 0 && nextOffset < items.Count ? EncodeCursor(nextOffset) : null
            }
        });
    }

    internal static Results<Ok<McpServerList>, NotFound<McpRegistryError>> ListServerVersions(
        IOptionsMonitor<McpRegistryOptions> options,
        string serverName,
        [FromQuery(Name = "include_deleted")] bool? includeDeleted) {
        var servers = options.CurrentValue.Servers;
        var name = Uri.UnescapeDataString(serverName);
        var latest = GetLatestVersions(servers);
        var versions = servers.Where(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))
                              .Where(x => includeDeleted == true || x.Status != McpServerStatus.Deleted)
                              .OrderByDescending(x => x, McpServerVersionComparer.Instance)
                              .Select(x => ToResponse(x, latest.Contains(x)))
                              .ToList();
        if (versions.Count == 0) {
            return TypedResults.NotFound(new McpRegistryError("Server not found"));
        }
        return TypedResults.Ok(new McpServerList {
            Servers = versions,
            Metadata = new McpServerListMetadata { Count = versions.Count }
        });
    }

    internal static Results<Ok<McpServerList>, NotFound<McpRegistryError>> ListServerVersionsUnencoded(
        IOptionsMonitor<McpRegistryOptions> options,
        string serverNamespace,
        string serverName,
        [FromQuery(Name = "include_deleted")] bool? includeDeleted) =>
        ListServerVersions(options, $"{serverNamespace}/{serverName}", includeDeleted);

    internal static Results<Ok<McpServerResponse>, NotFound<McpRegistryError>> GetServerVersion(
        IOptionsMonitor<McpRegistryOptions> options,
        string serverName,
        string version,
        [FromQuery(Name = "include_deleted")] bool? includeDeleted) {
        var servers = options.CurrentValue.Servers;
        var name = Uri.UnescapeDataString(serverName);
        var requestedVersion = Uri.UnescapeDataString(version);
        var latest = GetLatestVersions(servers);
        var candidates = servers.Where(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
        if (candidates.Count == 0) {
            return TypedResults.NotFound(new McpRegistryError("Server not found"));
        }
        var entry = LatestVersion.Equals(requestedVersion, StringComparison.OrdinalIgnoreCase)
            ? candidates.FirstOrDefault(latest.Contains)
            : candidates.FirstOrDefault(x => string.Equals(x.Version, requestedVersion, StringComparison.OrdinalIgnoreCase));
        if (entry is null || (entry.Status == McpServerStatus.Deleted && includeDeleted != true)) {
            return TypedResults.NotFound(new McpRegistryError("Server version not found"));
        }
        return TypedResults.Ok(ToResponse(entry, latest.Contains(entry)));
    }

    internal static Results<Ok<McpServerResponse>, NotFound<McpRegistryError>> GetServerVersionUnencoded(
        IOptionsMonitor<McpRegistryOptions> options,
        string serverNamespace,
        string serverName,
        string version,
        [FromQuery(Name = "include_deleted")] bool? includeDeleted) =>
        GetServerVersion(options, $"{serverNamespace}/{serverName}", version, includeDeleted);

    private static McpServerResponse ToResponse(McpServerEntry entry, bool isLatest) => new() {
        Server = entry,
        Meta = new McpServerResponseMeta {
            Official = new McpOfficialMeta {
                Status = entry.Status.ToString().ToLowerInvariant(),
                StatusMessage = entry.StatusMessage,
                StatusChangedAt = entry.StatusChangedAt,
                PublishedAt = entry.PublishedAt,
                UpdatedAt = entry.UpdatedAt,
                IsLatest = isLatest
            }
        }
    };

    /// <summary>Resolves the latest version of every server. Deleted versions are only considered when no other version exists.</summary>
    private static HashSet<McpServerEntry> GetLatestVersions(IEnumerable<McpServerEntry> servers) {
        var latest = new HashSet<McpServerEntry>(ReferenceEqualityComparer.Instance);
        foreach (var group in servers.Where(x => x.Name is not null).GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)) {
            var candidates = group.Where(x => x.Status != McpServerStatus.Deleted).ToList();
            if (candidates.Count == 0) {
                candidates = [.. group];
            }
            // On ties the entry declared last in configuration wins.
            latest.Add(candidates.Aggregate((max, next) => McpServerVersionComparer.Instance.Compare(next, max) >= 0 ? next : max));
        }
        return latest;
    }

    private static DateTimeOffset? GetLastModified(McpServerEntry entry) => new[] { entry.UpdatedAt, entry.StatusChangedAt, entry.PublishedAt }.Max();

    private static string EncodeCursor(int offset) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(offset.ToString(CultureInfo.InvariantCulture))).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static bool TryDecodeCursor(string? cursor, out int offset) {
        offset = 0;
        if (string.IsNullOrEmpty(cursor)) {
            return true;
        }
        try {
            var base64 = cursor.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
            var text = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out offset);
        } catch (FormatException) {
            return false;
        }
    }
}

/// <summary>Orders server entries by semantic version, falling back to publish date for non semantic versions.</summary>
internal sealed class McpServerVersionComparer : IComparer<McpServerEntry>
{
    public static readonly McpServerVersionComparer Instance = new();

    public int Compare(McpServerEntry? x, McpServerEntry? y) {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;
        if (SemanticVersion.TryParse(x.Version, out var vx) && SemanticVersion.TryParse(y.Version, out var vy)) {
            var result = vx.CompareTo(vy);
            if (result != 0) {
                return result;
            }
        }
        return Nullable.Compare(x.PublishedAt, y.PublishedAt);
    }

    private readonly record struct SemanticVersion(int[] Core, string[] PreRelease)
    {
        public static bool TryParse(string? value, out SemanticVersion version) {
            version = default;
            if (string.IsNullOrWhiteSpace(value)) {
                return false;
            }
            var text = value.Trim().TrimStart('v', 'V');
            var buildIndex = text.IndexOf('+');
            if (buildIndex >= 0) {
                text = text[..buildIndex];
            }
            var preReleaseIndex = text.IndexOf('-');
            var core = preReleaseIndex >= 0 ? text[..preReleaseIndex] : text;
            var preRelease = preReleaseIndex >= 0 ? text[(preReleaseIndex + 1)..].Split('.') : [];
            var parts = core.Split('.');
            if (parts.Length is < 1 or > 4) {
                return false;
            }
            var numbers = new int[4];
            for (var i = 0; i < parts.Length; i++) {
                if (!int.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out numbers[i])) {
                    return false;
                }
            }
            version = new SemanticVersion(numbers, preRelease);
            return true;
        }

        public int CompareTo(SemanticVersion other) {
            for (var i = 0; i < Core.Length; i++) {
                var result = Core[i].CompareTo(other.Core[i]);
                if (result != 0) return result;
            }
            // A release version has higher precedence than a pre-release one.
            if (PreRelease.Length == 0 || other.PreRelease.Length == 0) {
                return other.PreRelease.Length.CompareTo(PreRelease.Length);
            }
            for (var i = 0; i < Math.Min(PreRelease.Length, other.PreRelease.Length); i++) {
                var leftIsNumber = int.TryParse(PreRelease[i], NumberStyles.None, CultureInfo.InvariantCulture, out var left);
                var rightIsNumber = int.TryParse(other.PreRelease[i], NumberStyles.None, CultureInfo.InvariantCulture, out var right);
                var result = (leftIsNumber, rightIsNumber) switch {
                    (true, true) => left.CompareTo(right),
                    (true, false) => -1,
                    (false, true) => 1,
                    _ => string.CompareOrdinal(PreRelease[i], other.PreRelease[i])
                };
                if (result != 0) return result;
            }
            return PreRelease.Length.CompareTo(other.PreRelease.Length);
        }
    }
}
