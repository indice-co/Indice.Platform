using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Indice.Extensions;

namespace Indice.Services
{
    /// <summary>
    /// In memory <see cref="IFileService"/> implementation. Used to mock a file storage.
    /// </summary>
    public class FileServiceInMemory : IFileService
    {
        private readonly Dictionary<string, byte[]> Cache = new Dictionary<string, byte[]>();

        /// <summary>
        /// Deletes a file from store.
        /// </summary>
        /// <param name="filepath">The file path</param>
        /// <param name="isDirectory"></param>
        public Task<bool> DeleteAsync(string filepath, bool isDirectory = false) {
            GuardExists(filepath);
            if (!isDirectory)
                Cache.Remove(filepath);
            else {
                foreach (var path in Cache.Keys.Where(x => x.StartsWith(filepath)).ToArray())
                    Cache.Remove(path);
            }
            return Task.FromResult(true);
        }

        /// <summary>
        /// Gets the file data in bytes
        /// </summary>
        /// <param name="filepath">The file path.</param>
        public Task<byte[]> GetAsync(string filepath) {
            GuardExists(filepath);
            return Task.FromResult(Cache[filepath]);
        }

        /// <summary>
        /// Gets a path list. For a given folder
        /// </summary>
        /// <param name="path">The file path.</param>
        public Task<IEnumerable<string>> SearchAsync(string path) {
            if (string.IsNullOrWhiteSpace(path)) {
                return Task.FromResult(Cache.Keys.AsEnumerable());
            }
            return Task.FromResult(Cache.Keys.Where(x => x.ToLower().StartsWith(path.ToLower())));
        }

        /// <summary>
        /// Gets metadata for a file.
        /// </summary>
        /// <param name="filepath">The file path.</param>
        public Task<FileProperties> GetPropertiesAsync(string filepath) {
            GuardExists(filepath);
            var data = Cache[filepath];
            var props = new FileProperties {
                Length = data.Length,
                LastModified = DateTime.UtcNow,
                ContentType = FileExtensions.GetMimeType(Path.GetExtension(filepath)),
                ContentDisposition = $"attachment; filename={Path.GetFileName(filepath)}",
            };
            return Task.FromResult(props);
        }

        /// <summary>
        /// Save a file to store. Update or create the resource.
        /// </summary>
        /// <param name="filepath">The file path.</param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public Task SaveAsync(string filepath, Stream stream) {
            if (!Cache.ContainsKey(filepath)) {
                Cache[filepath] = null;
            }
            using (var ms = new MemoryStream()) {
                stream.CopyTo(ms);
                Cache[filepath] = ms.ToArray();
            }
#if NET452
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }

        private void GuardExists(string filepath) {
            if (!Cache.ContainsKey(filepath)) {
                throw new Exception($"file '{filepath}' not found");
            }
        }
    }
}
