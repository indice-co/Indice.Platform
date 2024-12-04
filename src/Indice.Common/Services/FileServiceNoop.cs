namespace Indice.Services;

/// <summary>A default implementation for <see cref="IFileService"/> that does nothing.</summary>
public class FileServiceNoop : IFileService
{
    /// <inheritdoc/>
    public Task<bool> DeleteAsync(string filePath, bool isDirectory = false) => Task.FromResult(false);

    /// <inheritdoc/>
    public Task<byte[]> GetAsync(string filePath) => Task.FromResult(Array.Empty<byte>());

    /// <inheritdoc/>
    public Task<FileProperties?> GetPropertiesAsync(string filePath) => Task.FromResult(default(FileProperties));

    /// <inheritdoc/>
    public Task SaveAsync(string filePath, Stream stream, FileServiceSaveOptions? saveOptions) => Task.CompletedTask;
    
    /// <inheritdoc/>
    public Task MoveAsync(string sourcePath, string destinationPath) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<IEnumerable<string>> SearchAsync(string path) => Task.FromResult(Enumerable.Empty<string>());
}
