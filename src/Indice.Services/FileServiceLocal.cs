using Indice.Extensions;

namespace Indice.Services;

/// <summary>Local file system implementation for <see cref="IFileService"/>.</summary>
public class FileServiceLocal : IFileService
{
    /// <summary>Creates a new instance of <see cref="FileServiceLocal"/>.</summary>
    /// <param name="baseDirectoryPath">the base directory to put all files.</param>
    public FileServiceLocal(string baseDirectoryPath) {
        BaseDirectoryPath = baseDirectoryPath?.TrimEnd('/', '\\') ?? throw new ArgumentNullException(nameof(baseDirectoryPath));
        if (!Path.IsPathRooted(baseDirectoryPath)) {
            throw new ArgumentOutOfRangeException(nameof(baseDirectoryPath), $"Path '{baseDirectoryPath}' must be rooted");
        }
    }

    /// <summary>Base directory path for files.</summary>
    protected string BaseDirectoryPath { get; }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string filePath, bool isDirectory = false) {
        filePath = Path.Combine(BaseDirectoryPath, filePath);
        GuardExists(filePath, isDirectory);
        if (!isDirectory) {
            File.Delete(filePath);
        } else {
            foreach (var directory in Directory.EnumerateDirectories(filePath)) {
                foreach (var file in Directory.EnumerateFiles(directory)) {
                    File.Delete(file);
                }
                Directory.Delete(directory);
            }
            foreach (var file in Directory.EnumerateFiles(filePath)) {
                File.Delete(file);
            }
        }
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<byte[]> GetAsync(string filePath) {
        filePath = Path.Combine(BaseDirectoryPath, filePath);
        GuardExists(filePath);
        return Task.FromResult(File.ReadAllBytes(filePath));
    }

    /// <inheritdoc />
    public Task<IEnumerable<string>> SearchAsync(string path) {
        var folderPath = Path.Combine(BaseDirectoryPath, path);
        if (!GuardExists(folderPath, isDirectory: true, throwOnError: false)) {
            return Task.FromResult(Enumerable.Empty<string>());
        }
        var results = new List<string>();
        foreach (var directory in Directory.EnumerateDirectories(folderPath)) {
            foreach (var file in Directory.EnumerateFiles(directory)) {
                results.Add(file.Replace(BaseDirectoryPath, string.Empty));
            }
        }
        foreach (var file in Directory.EnumerateFiles(folderPath)) {
            results.Add(file.Replace(BaseDirectoryPath, string.Empty));
        }
        return Task.FromResult(results.AsEnumerable());
    }

    /// <inheritdoc />
    public Task<FileProperties> GetPropertiesAsync(string filePath) {
        filePath = Path.Combine(BaseDirectoryPath, filePath);
        GuardExists(filePath);
        var info = new FileInfo(filePath);
        return Task.FromResult(new FileProperties {
            Length = info.Length,
            LastModified = info.LastWriteTimeUtc,
            ContentType = FileExtensions.GetMimeType(info.Extension),
            ContentDisposition = $"attachment; filename={Path.GetFileName(info.FullName)}",
            ContentHash = null,
            ETag = $"\"{info.LastWriteTimeUtc.Ticks}\""
        });
    }

    /// <inheritdoc />
    public async Task SaveAsync(string filePath, Stream stream, FileServiceSaveOptions saveOptions) {
        filePath = Path.Combine(BaseDirectoryPath, filePath);
        var directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory)) {
            Directory.CreateDirectory(directory);
        }
        using (var fs = File.Open(filePath, FileMode.Create)) {
            await stream.CopyToAsync(fs);
        }
    }

    private static bool GuardExists(string path, bool isDirectory = false, bool throwOnError = true) {
        var exists = isDirectory ? Directory.Exists(path) : File.Exists(path);
        if (!exists && throwOnError) {
            throw new Exception($"file or directory '{path}' not found.");
        }
        return exists;
    }
}

/// <summary>File service options specific to File System local.</summary>
public class FileServiceLocalOptions
{
    /// <summary>The path to use for storing the images. Can be a relative path or absolute. </summary>
    public string Path { get; set; } = "App_Data";
}
