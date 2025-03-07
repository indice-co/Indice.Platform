using Indice.Extensions;

namespace Indice.Services;

/// <summary>In memory <see cref="IFileService"/> implementation. Used to mock a file storage.</summary>
public class FileServiceInMemory : IFileService
{
    private readonly Dictionary<string, byte[]?> Cache = new();

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string filePath, bool isDirectory = false) {
        GuardExists(filePath);
        if (!isDirectory)
            Cache.Remove(filePath);
        else {
            foreach (var path in Cache.Keys.Where(x => x.StartsWith(filePath)).ToArray())
                Cache.Remove(path);
        }
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<byte[]> GetAsync(string filePath) {
        GuardExists(filePath);
        return Task.FromResult(Cache[filePath]!);
    }

    /// <inheritdoc />
    public Task<IEnumerable<string>> SearchAsync(string path) {
        if (string.IsNullOrWhiteSpace(path)) {
            return Task.FromResult(Cache.Keys.AsEnumerable());
        }
        return Task.FromResult(Cache.Keys.Where(x => x.ToLowerInvariant().StartsWith(path.ToLowerInvariant())));
    }

    /// <inheritdoc />
    public Task<FileProperties?> GetPropertiesAsync(string filePath) {
        GuardExists(filePath);
        var data = Cache[filePath];
        var props = new FileProperties {
            Length = data!.Length,
            LastModified = DateTime.UtcNow,
            ContentType = FileExtensions.GetMimeType(Path.GetExtension(filePath)),
            ContentDisposition = $"attachment; filename={Path.GetFileName(filePath)}",
        };
        return Task.FromResult(props)!;
    }

    /// <inheritdoc />
    public Task SaveAsync(string filePath, Stream stream, FileServiceSaveOptions? saveOptions) {
        if (!Cache.ContainsKey(filePath)) {
            Cache[filePath] = null;
        }
        using (var ms = new MemoryStream()) {
            stream.CopyTo(ms);
            Cache[filePath] = ms.ToArray();
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task MoveAsync(string sourcePath, string destinationPath) {
        var pathsToMove = (await SearchAsync(sourcePath)).ToArray();
        foreach (var path in pathsToMove) {
            if (Cache.TryGetValue(path, out var result)) {
                var newPath = path.Replace(sourcePath, destinationPath);
                Cache.TryAdd(newPath, result);
                Cache.Remove(path);
            }
        }
    }

    private void GuardExists(string filePath) {
        if (!Cache.ContainsKey(filePath)) {
            throw new Exception($"file '{filePath}' not found");
        }
    }
}
