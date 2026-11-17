using System.Diagnostics.CodeAnalysis;

namespace Indice.Features.Media.AspNetCore;

/// <summary>Confines caller supplied paths to the media root folder of the underlying file storage.</summary>
internal static class MediaRootPath
{
    private static readonly char[] ForbiddenCharacters = ['\\', ':', '?', '#', '%'];

    /// <summary>
    /// Validates a path relative to the media root (as received from the route, already URL decoded) and
    /// returns the corresponding storage path. The path is rejected when it contains any construct that
    /// could make the downstream storage client resolve it outside of the media root, such as dot segments,
    /// backslashes (treated as separators by <see cref="Uri"/>), drive or stream separators, URI delimiters
    /// or residual percent encoding that a later layer could decode again.
    /// </summary>
    /// <param name="rootFolder">The root folder in the file storage under which all files are stored.</param>
    /// <param name="path">The caller supplied path, relative to the media root.</param>
    /// <param name="storagePath">The validated storage path, always starting with <c>media/</c>.</param>
    /// <returns><see langword="true"/> if the path is safe to use; otherwise <see langword="false"/>.</returns>
    internal static bool TryGetStoragePath(string? path, string rootFolder, [NotNullWhen(true)] out string? storagePath) {
        storagePath = null;
        if (string.IsNullOrWhiteSpace(path)) {
            return false;
        }
        if (path.IndexOfAny(ForbiddenCharacters) >= 0 || path.Any(char.IsControl)) {
            return false;
        }
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0) {
            return false;
        }
        foreach (var segment in segments) {
            // Rejects '.', '..' and variants padded with dots or whitespace that some file systems normalize back to them.
            if (segment.Trim().Trim('.').Trim().Length == 0) {
                return false;
            }
        }
        storagePath = string.Join('/', segments);
        if (!string.IsNullOrEmpty(rootFolder)) {
            storagePath = $"{rootFolder}/{storagePath}";
        }
        return true;
    }
}
