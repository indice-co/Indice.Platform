using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Indice.Extensions;

namespace Indice.Services
{
    /// <summary>
    /// Local file system implementation for <see cref="IFileService"/>
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

        /// <summary>
        /// Delete from file system
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="isDirectory"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Retrieve data
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public Task<byte[]> GetAsync(string filepath) {
            filepath = Path.Combine(BaseDirectoryPath, filepath);
            GuardExists(filepath);
            return Task.FromResult(File.ReadAllBytes(filepath));
        }

        /// <summary>
        /// Gets a path list. For a given folder
        /// </summary>
        /// <param name="path">The file path</param>
        /// <returns></returns>
        public Task<IEnumerable<string>> SearchAsync(string path) {
            var folderpath = Path.Combine(BaseDirectoryPath, path);
            if (!GuardExists(folderpath, isDirectory: true, throwOnError: false)) {
                return Task.FromResult(Enumerable.Empty<string>());
            }
            var results = new List<string>();
            foreach (var directory in Directory.EnumerateDirectories(folderpath)) {
                foreach (var file in Directory.EnumerateFiles(directory)) {
                    results.Add(file.Replace(BaseDirectoryPath, ""));
                }
            }
            foreach (var file in Directory.EnumerateFiles(folderpath)) {
                results.Add(file.Replace(BaseDirectoryPath, ""));
            }
            return Task.FromResult(results.AsEnumerable());
        }

        /// <summary>
        /// Gets the file meta-data.
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public Task<FileProperties> GetPropertiesAsync(string filepath) {
            filepath = Path.Combine(BaseDirectoryPath, filepath);
            GuardExists(filepath);
            var info = new FileInfo(filepath);
            return Task.FromResult(new FileProperties {
                Length = info.Length,
                LastModified = info.LastWriteTimeUtc,
                ContentType = FileExtensions.GetMimeType(info.Extension),
                ContentDisposition = $"attachment; filename={Path.GetFileName(info.FullName)}",
                ContentHash = null,
                ETag = $"\"{info.LastWriteTimeUtc.Ticks}\"",
            });
        }

        /// <summary>
        /// Saves the stream to the file path.
        /// </summary>
        /// <param name="filepath">The file path</param>
        /// <param name="stream">Data Stream</param>
        /// <returns></returns>
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

        private bool GuardExists(string path, bool isDirectory = false, bool throwOnError = true) {
            var exists = isDirectory ? Directory.Exists(path) : File.Exists(path);
            if (!exists && throwOnError) {
                throw new Exception($"file or directory '{path}' not found.");
            }
            return exists;
        }
    }

    /// <summary>
    /// File service options specific to File System local.
    /// </summary>
    public class FileServiceLocalOptions
    {
        /// <summary>
        /// The path to use for storing the images. Can be a relative path or absolute. 
        /// </summary>
        public string Path { get; set; } = "App_Data";
    }
}
