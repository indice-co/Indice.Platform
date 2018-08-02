using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Indice.Extensions;

namespace Indice.Services
{
    /// <summary>
    /// Local Filesystem implementation for <see cref="IFileService"/>
    /// </summary>
    public class FileServiceLocal : IFileService
    {
        /// <summary>
        /// Base directory path for files.
        /// </summary>
        protected string BaseDirectoryPath { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="baseDirectoryPath">the base directory to put all files</param>
        public FileServiceLocal(string baseDirectoryPath) {
            BaseDirectoryPath = baseDirectoryPath?.TrimEnd('/', '\\') ?? throw new ArgumentNullException(nameof(baseDirectoryPath));
            if (!Path.IsPathRooted(baseDirectoryPath)) {
                throw new ArgumentOutOfRangeException(nameof(baseDirectoryPath), $"Path '{baseDirectoryPath}' must be rooted");
            }
        }
        
        public Task<bool> DeleteAsync(string filepath, bool isDirectory = false) {
            filepath = Path.Combine(BaseDirectoryPath, filepath);
            GuardExists(filepath, isDirectory);
            if (!isDirectory) {
                File.Delete(filepath);
            } else {
                foreach (var directory in Directory.EnumerateDirectories(filepath)) {
                    foreach (var file in Directory.EnumerateFiles(directory)) {
                        File.Delete(file);
                    }
                    Directory.Delete(directory);
                }
                foreach (var file in Directory.EnumerateFiles(filepath)) {
                    File.Delete(file);
                }
            }
            return Task.FromResult(true);
        }

        public Task<byte[]> GetAsync(string filepath) {
            filepath = Path.Combine(BaseDirectoryPath, filepath);
            GuardExists(filepath);
            return Task.FromResult(File.ReadAllBytes(filepath));
        }

        public Task<FileProperties> GetPropertiesAsync(string filepath) {
            filepath = Path.Combine(BaseDirectoryPath, filepath);
            GuardExists(filepath);
            var info = new FileInfo(filepath);
            return Task.FromResult(new FileProperties {
                Length = info.Length,
                LastModified = info.LastWriteTimeUtc,
                ContentType = FileExtensions.GetMimeType(info.Extension),
                ContentDisposition = $"attachment; filename={Path.GetFileName(info.FullName)}",
                ContentMD5 = null,
                ETag = $"\"{info.LastWriteTimeUtc.Ticks}\"",
            });
        }

        public async Task SaveAsync(string filepath, Stream stream) {
            filepath = Path.Combine(BaseDirectoryPath, filepath);
            var directory = Path.GetDirectoryName(filepath);
            if (!Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }
            using (var fs = File.Open(filepath, FileMode.Create)) {
                await stream.CopyToAsync(fs);
            }
        }

        private void GuardExists(string path, bool isDirectory = false) {
            var exists = isDirectory ? Directory.Exists(path) : File.Exists(path);
            if (!exists)
                throw new Exception($"file or directory '{path}' not found");
        }

        private void GuardRelative(string path) {
            if (Path.IsPathRooted(path))
                throw new Exception($"The path must be relative '{path}'");
        }
    }
}
