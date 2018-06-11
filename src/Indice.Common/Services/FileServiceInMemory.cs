using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indice.Services
{
    public class FileServiceInMemory : IFileService
    {
        private readonly Dictionary<string, byte[]> Cache = new Dictionary<string, byte[]>();

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

        public Task<byte[]> GetAsync(string filepath) {
            GuardExists(filepath);
            return Task.FromResult(Cache[filepath]);
        }

        public Task<FileProperties> GetPropertiesAsync(string filepath) {
            GuardExists(filepath);
            var data = Cache[filepath];
            var props = new FileProperties {
                Length = data.Length,
                LastModified = DateTime.UtcNow
            };
            return Task.FromResult(props);
        }

        public Task SaveAsync(string filepath, Stream stream) {
            if (!Cache.ContainsKey(filepath)) {
                Cache[filepath] = null;
            }
            using (MemoryStream ms = new MemoryStream()) {
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
            if (!Cache.ContainsKey(filepath))
                throw new Exception($"file '{filepath}' not found");
        }
    }
}
