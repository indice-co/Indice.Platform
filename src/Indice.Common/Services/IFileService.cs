using System.Net.Mime;
using System.Text.Json;

namespace Indice.Services;

/// <summary>File storage abstraction.</summary>
public interface IFileService
{
    /// <summary>Saves a file.</summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="stream">The file content as a <see cref="Stream"/>.</param>
    /// <param name="saveOptions">Options when saving a stream through <see cref="IFileService"/>.</param>
    Task SaveAsync(string filePath, Stream stream, FileServiceSaveOptions? saveOptions);
    /// <summary>Retrieves a file using a path.</summary>
    /// <param name="filePath">The file path.</param>
    Task<byte[]> GetAsync(string filePath);
    /// <summary>Gets a path list. For a given folder.</summary>
    /// <param name="path">The file path.</param>
    Task<IEnumerable<string>> SearchAsync(string path);
    /// <summary>Gets the file properties.</summary>
    /// <param name="filePath">The file path.</param>
    Task<FileProperties?> GetPropertiesAsync(string filePath);
    /// <summary>Removes the file from storage. In case of a folder or virtual path prefix it will remove all files recursive.</summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="isDirectory">Indicates that the path is a directory.</param>
    Task<bool> DeleteAsync(string filePath, bool isDirectory = false);
    /// <summary>Will move all file instances from the location in the <paramref name="sourcePath"/> to the <paramref name="destinationPath"/></summary>
    /// <param name="sourcePath">The source path to move. </param>
    /// <param name="destinationPath"></param>
    /// <remarks>If the path in source is a directory structure it will move all contents to be relocated to the destination path. If not it will move only a single file</remarks>
    /// <returns>The task handle</returns>
    Task MoveAsync(string sourcePath, string destinationPath);
}

/// <summary>Exception class for <see cref="IFileService"/> related exceptions.</summary>
public class FileServiceException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="FileServiceException"/> class with a specified error message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public FileServiceException(string message) : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="FileServiceException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.</summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="inner">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public FileServiceException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>Exception class for <see cref="IFileService"/> related exceptions.</summary>
public class FileNotFoundServiceException : FileServiceException
{
    /// <summary>Initializes a new instance of the <see cref="FileNotFoundServiceException"/> class with a specified error message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public FileNotFoundServiceException(string message) : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="FileNotFoundServiceException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.</summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="inner">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public FileNotFoundServiceException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>File properties. Metadata class.</summary>
public class FileProperties
{
    /// <summary>Last modified.</summary>
    public DateTimeOffset? LastModified { get; set; }
    /// <summary>Etag.</summary>
    public string? ETag { get; set; }
    /// <summary>Media type (i.e. application/octet-stream).</summary>
    public string ContentType { get; set; } = "application/octet-stream";
    /// <summary>Content MD5 hash.</summary>
    public string? ContentHash { get; set; }
    /// <summary>Size in bytes.</summary>
    public long Length { get; set; }
    /// <summary>Content language.</summary>
    public string? ContentLanguage { get; set; }
    /// <summary>Content encoding.</summary>
    public string? ContentEncoding { get; set; }
    /// <summary>Content disposition.</summary>
    public string? ContentDisposition { get; set; }
    /// <summary>Cache control.</summary>
    public string? CacheControl { get; set; }
}

/// <summary>Extensions for <see cref="IFileService"/>.</summary>
public static class FileServiceExtensions
{
    /// <summary>Saves file data given a byte array instead of a stream.</summary>
    /// <param name="fileService">File storage abstraction.</param>
    /// <param name="filePath">The file path.</param>
    /// <param name="bytes">The file content as a <see cref="byte"/> array.</param>
    public static async Task SaveAsync(this IFileService fileService, string filePath, byte[] bytes) {
        using var stream = new MemoryStream(bytes);
        await fileService.SaveAsync(filePath, stream, saveOptions: null);
    }

    /// <summary>Saves file data with no options.</summary>
    /// <param name="fileService">File storage abstraction.</param>
    /// <param name="filePath">The file path.</param>
    /// <param name="stream">The file content as a <see cref="Stream"/>.</param>
    public static async Task SaveAsync(this IFileService fileService, string filePath, Stream stream) =>
        await fileService.SaveAsync(filePath, stream, saveOptions: null);

    /// <summary>Saves file data using a specified content type.</summary>
    /// <param name="fileService">File storage abstraction.</param>
    /// <param name="filePath">The file path.</param>
    /// <param name="stream">The file content as a <see cref="Stream"/>.</param>
    /// <param name="contentType">The MIME content type of the blob.</param>
    public static async Task SaveAsync(this IFileService fileService, string filePath, Stream stream, string contentType) =>
        await fileService.SaveAsync(filePath, stream, new FileServiceSaveOptions {
            ContentType = contentType
        });

    /// <summary>Saves file data as serialized json.</summary>
    /// <typeparam name="T">The type of data to save.</typeparam>
    /// <param name="fileService">File storage abstraction.</param>
    /// <param name="path">The file path.</param>
    /// <param name="payload">The object to save.</param>
    /// <param name="jsonOptions">Provides options to be used with <see cref="JsonSerializer"/>.</param>
    /// <param name="saveOptions">Options when saving a stream through <see cref="IFileService"/>.</param>
    public static async Task SaveAsync<T>(this IFileService fileService, string path, T payload, JsonSerializerOptions jsonOptions, FileServiceSaveOptions? saveOptions = null) where T : class {
        saveOptions ??= new FileServiceSaveOptions();
        using (var stream = new MemoryStream()) {
            await JsonSerializer.SerializeAsync(stream, payload, jsonOptions);
            stream.Seek(0, SeekOrigin.Begin);
            if (string.IsNullOrWhiteSpace(saveOptions.ContentType)) {
                saveOptions.ContentType = MediaTypeNames.Application.Json;
            }
            await fileService.SaveAsync(path, stream, saveOptions);
        }
    }
}

/// <summary>Options when saving a stream through <see cref="IFileService"/>.</summary>
public class FileServiceSaveOptions
{
    /// <summary>The MIME content type of the blob.</summary>
    public string? ContentType { get; set; }
    /// <summary>Specify directives for caching mechanisms.</summary>
    public string? CacheControl { get; set; }

    /// <summary>Check if options are empty.</summary>
    /// <returns></returns>
    public bool IsEmpty() => string.IsNullOrEmpty(ContentType) &&
                             string.IsNullOrEmpty(CacheControl);
}