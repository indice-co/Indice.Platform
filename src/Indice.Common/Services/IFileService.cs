using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Indice.Services
{
    /// <summary>
    /// File storage abstraction.
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Saves a file.
        /// </summary>
        /// <param name="filepath">The file path.</param>
        /// <param name="stream">The file content as a <see cref="Stream"/>.</param>
        Task SaveAsync(string filepath, Stream stream);
        /// <summary>
        /// Retrieves a file using a path.
        /// </summary>
        /// <param name="filepath">The file path.</param>
        Task<byte[]> GetAsync(string filepath);
        /// <summary>
        /// Gets a path list. For a given folder
        /// </summary>
        /// <param name="path">The file path.</param>
        Task<IEnumerable<string>> SearchAsync(string path);
        /// <summary>
        /// Gets the file properties.
        /// </summary>
        /// <param name="filepath">The file path.</param>
        Task<FileProperties> GetPropertiesAsync(string filepath);
        /// <summary>
        /// Removes the file from storage. In case of a folder or virtual path prefix it will remove all files recursive.
        /// </summary>
        /// <param name="filepath">The file path.</param>
        /// <param name="isDirectory">Indicates that the path is a directory.</param>
        Task<bool> DeleteAsync(string filepath, bool isDirectory = false);
    }

    /// <summary>
    /// Exception class for <see cref="IFileService"/> related exceptions.
    /// </summary>
    public class FileServiceException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileServiceException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public FileServiceException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileServiceException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public FileServiceException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Exception class for <see cref="IFileService"/> related exceptions.
    /// </summary>
    public class FileNotFoundServiceException : FileServiceException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileNotFoundServiceException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public FileNotFoundServiceException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileNotFoundServiceException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public FileNotFoundServiceException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// File properties. Metadata class.
    /// </summary>
    public class FileProperties
    {
        /// <summary>
        /// Last modified.
        /// </summary>
        public DateTimeOffset? LastModified { get; set; }
        /// <summary>
        /// Etag.
        /// </summary>
        public string ETag { get; set; }
        /// <summary>
        /// Media type (i.e. application/octet-stream).
        /// </summary>
        public string ContentType { get; set; }
        /// <summary>
        /// Content MD5 hash.
        /// </summary>
        public string ContentHash { get; set; }
        /// <summary>
        /// Size in bytes.
        /// </summary>
        public long Length { get; set; }
        /// <summary>
        /// Content language.
        /// </summary>
        public string ContentLanguage { get; set; }
        /// <summary>
        /// Content encoding.
        /// </summary>
        public string ContentEncoding { get; set; }
        /// <summary>
        /// Content disposition.
        /// </summary>
        public string ContentDisposition { get; set; }
        /// <summary>
        /// Cache control.
        /// </summary>
        public string CacheControl { get; set; }
    }

    /// <summary>
    /// Extensions for <see cref="IFileService"/>
    /// </summary>
    public static class FileServiceExtensions
    {
        /// <summary>
        /// Save file data given a byte array instead of a stream.
        /// </summary>
        /// <param name="fileService"></param>
        /// <param name="filepath"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static async Task SaveAsync(this IFileService fileService, string filepath, byte[] bytes) {
            using var stream = new MemoryStream(bytes);
            await fileService.SaveAsync(filepath, stream);
        }
    }
}
