using System;
using System.IO;
using System.Threading.Tasks;

namespace Indice.Services
{
    public interface IFileService
    {
        Task SaveAsync(string filepath, Stream stream);
        Task<byte[]> GetAsync(string filepath);
        Task<FileProperties> GetPropertiesAsync(string filepath);
        Task<bool> DeleteAsync(string filepath, bool isDirectory = false);
    }

    public class FileServiceException : Exception
    {
        public FileServiceException(string message) : base(message) { }

        public FileServiceException(string message, Exception inner) : base(message, inner) { }
    }

    public class FileNotFoundServiceException : FileServiceException
    {
        public FileNotFoundServiceException(string message) : base(message) { }

        public FileNotFoundServiceException(string message, Exception inner) : base(message, inner) { }
    }

    public class FileProperties
    {
        public DateTimeOffset? LastModified { get; set; }
        public string ETag { get; set; }
        public string ContentType { get; set; }
        public string ContentMD5 { get; set; }
        public long Length { get; set; }
        public string ContentLanguage { get; set; }
        public string ContentEncoding { get; set; }
        public string ContentDisposition { get; set; }
        public string CacheControl { get; set; }
    }

    public static class FileServiceExtensions
    {
        public static async Task SaveAsync(this IFileService fileService, string filepath, byte[] bytes) {
            using (var stream = new MemoryStream(bytes)) {
                await fileService.SaveAsync(filepath, stream);
            }
        }
    }
}
