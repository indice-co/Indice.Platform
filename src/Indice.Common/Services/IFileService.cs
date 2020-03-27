using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Indice.Services
{
    /// <summary>
    /// File storage abstraction
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Saves a file.
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        Task SaveAsync(string filepath, Stream stream);
        /// <summary>
        /// Retrieves a file using a path.
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        Task<byte[]> GetAsync(string filepath);
        /// <summary>
        /// Gets a path list. For a given folder
        /// </summary>
        /// <param name="path">The file path</param>
        /// <returns></returns>
        Task<IEnumerable<string>> SearchAsync(string path);
        /// <summary>
        /// Gets the file properties
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        Task<FileProperties> GetPropertiesAsync(string filepath);
        /// <summary>
        /// Removes the file from storage. In case of a folder or virtual path prefix it will remove all files recursive.
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="isDirectory">Indicates the path is a directory</param>
        /// <returns></returns>
        Task<bool> DeleteAsync(string filepath, bool isDirectory = false);
    }

    /// <summary>
    /// Exception class for <see cref="IFileService"/> related exceptions.
    /// </summary>
    public class FileServiceException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message"></param>
        public FileServiceException(string message) : base(message) { }

        /// <summary>
        /// Constructs a new <see cref="FileServiceException"/> wrapping an other exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public FileServiceException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// File not found exception.
    /// </summary>
    public class FileNotFoundServiceException : FileServiceException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message"></param>
        public FileNotFoundServiceException(string message) : base(message) { }

        /// <summary>
        /// Constructs a new <see cref="FileNotFoundServiceException"/> wrapping an other exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public FileNotFoundServiceException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// File properties. Metadata class
    /// </summary>
    public class FileProperties
    {
        /// <summary>
        /// Last modified.
        /// </summary>
        public DateTimeOffset? LastModified { get; set; }
        /// <summary>
        /// Etag
        /// </summary>
        public string ETag { get; set; }
        /// <summary>
        /// Media type (ie application/octet-stream)
        /// </summary>
        public string ContentType { get; set; }
        /// <summary>
        /// Content MD5 hash
        /// </summary>
        public string ContentMD5 { get; set; }
        /// <summary>
        /// Size in Bytes
        /// </summary>
        public long Length { get; set; }
        /// <summary>
        /// Content language
        /// </summary>
        public string ContentLanguage { get; set; }
        /// <summary>
        /// Content encoding
        /// </summary>
        public string ContentEncoding { get; set; }
        /// <summary>
        /// Content Disposition
        /// </summary>
        public string ContentDisposition { get; set; }
        /// <summary>
        /// Cache control
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
            using (var stream = new MemoryStream(bytes)) {
                await fileService.SaveAsync(filepath, stream);
            }
        }
    }
}
