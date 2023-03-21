namespace Indice.Services;

/// <summary>A default implementation for <see cref="IFileService"/> that does nothing.</summary>
public class FileServiceNoop : IFileService
{
    /// <inheritdoc/>
    public Task<bool> DeleteAsync(string filepath, bool isDirectory = false) => Task.FromResult(false);

    /// <inheritdoc/>
    public Task<byte[]> GetAsync(string filepath) => Task.FromResult(Array.Empty<byte>());

    /// <inheritdoc/>
    public Task<FileProperties> GetPropertiesAsync(string filepath) => Task.FromResult(default(FileProperties));

    /// <inheritdoc/>
    public Task SaveAsync(string filepath, Stream stream) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<IEnumerable<string>> SearchAsync(string path) => Task.FromResult(Enumerable.Empty<string>());
}
