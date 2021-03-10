using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Indice.Types;
using Xunit;

namespace Indice.Services.Tests
{
    public class FileServiceAzureTests
    {
        private readonly string _connectionString = "UseDevelopmentStorage=true;DevelopmentStorageProxyUri=http://127.0.0.1";
        private readonly IFileService _FileService;

        public FileServiceAzureTests() {
            if (_connectionString.StartsWith("UseDevelopmentStorage=true;")) { 
                StorageEmulator.Start();
            }
            _FileService = new FileServiceAzureStorage(_connectionString, "test");
        }
        [Fact]
        public async Task UploadFile() {
            var folder = new Base64Id(Guid.NewGuid());
            var filename = $"{new Base64Id(Guid.NewGuid())}.txt";
            var contents = Encoding.UTF8.GetBytes($"This is the contents of the file. {DateTime.UtcNow:D}");
            await _FileService.SaveAsync($"uploads/{folder}/{filename}", contents);
            var properties = await _FileService.GetPropertiesAsync($"uploads/{folder}/{filename}");
            Assert.Equal("text/plain", properties.ContentType);
            Assert.Equal(contents.Length, properties.Length);
            await _FileService.SaveAsync($"uploads/{folder}/{filename}", Encoding.UTF8.GetBytes("Updated contents"));
            await _FileService.DeleteAsync($"uploads");
        }

        [Fact]
        public async Task GetFileTest() {
            var folder = new Base64Id(Guid.NewGuid());
            var filename = $"{new Base64Id(Guid.NewGuid())}.txt";
            await _FileService.SaveAsync($"getfiles/{folder}/{filename}", Encoding.UTF8.GetBytes($"This is the contents of the file. {DateTime.UtcNow:D}"));
            var data = await _FileService.GetAsync($"getfiles/{folder}/{filename}");
            var contents = Encoding.UTF8.GetString(data);
            Assert.StartsWith("This is the contents of the file", contents);
            await _FileService.DeleteAsync($"getfiles");
        }

        [Fact]
        public async Task GetDirectoryListTest() {
            var folder = new Base64Id(Guid.NewGuid());
            await _FileService.SaveAsync($"listing/{folder}/{new Base64Id(Guid.NewGuid())}.txt", Encoding.UTF8.GetBytes($"This is the contents of the file. {DateTime.UtcNow:D}"));
            await _FileService.SaveAsync($"listing/{folder}/{new Base64Id(Guid.NewGuid())}.txt", Encoding.UTF8.GetBytes($"This is the contents of the file. {DateTime.UtcNow:D}"));
            await _FileService.SaveAsync($"listing/{folder}/{new Base64Id(Guid.NewGuid())}.txt", Encoding.UTF8.GetBytes($"This is the contents of the file. {DateTime.UtcNow:D}"));
            await _FileService.SaveAsync($"listing/{folder}/{new Base64Id(Guid.NewGuid())}.txt", Encoding.UTF8.GetBytes($"This is the contents of the file. {DateTime.UtcNow:D}"));
            var list = await _FileService.SearchAsync($"listing/{folder}/");
            var list2 = await _FileService.SearchAsync($"listing/{folder}");
            var list3 = await _FileService.SearchAsync($"listing");
            await _FileService.DeleteAsync($"listing");
            Assert.Equal(4, list.Count());
        }

        [Fact]
        public async Task DeleteFileTest() {
            var folder = new Base64Id(Guid.NewGuid());
            var filename = $"{new Base64Id(Guid.NewGuid())}.txt";
            await _FileService.SaveAsync($"deletefiles/{folder}/{filename}", Encoding.UTF8.GetBytes($"This is the contents of the file. {DateTime.UtcNow:D}"));
            await _FileService.SaveAsync($"deletefiles/{folder}/2_{filename}", Encoding.UTF8.GetBytes($"This is the contents of the file. {DateTime.UtcNow:D}"));
            await _FileService.SaveAsync($"deletefiles/{folder}/3_{filename}", Encoding.UTF8.GetBytes($"This is the contents of the file. {DateTime.UtcNow:D}"));
            var ok = await _FileService.DeleteAsync($"deletefiles/{folder}/{filename}");
            Assert.True(ok);
            var ok2 = await _FileService.DeleteAsync($"deletefiles/{folder}/", isDirectory: true);
            Assert.True(ok2);
            var list = await _FileService.SearchAsync($"deletefiles/{folder}");
            await _FileService.DeleteAsync($"deletefiles");
            Assert.Empty(list);
        }

    }

    public static class StorageEmulator
    {
        public static void Start() {
            // check if emulator is already running
            var processes = Process.GetProcesses().OrderBy(p => p.ProcessName).ToList();
            if (processes.Any(process => process.ProcessName.Contains("DSServiceLDB"))) {
                return;
            }

            //var command = Environment.GetEnvironmentVariable("PROGRAMFILES") + @"\Microsoft SDKs\Windows Azure\Emulator\csrun.exe";
            const string command = @"C:\Program Files\Microsoft SDKs\Azure\Emulator\csrun.exe";

            using (var process = Process.Start(command, "/devstore:start")) {
                process.WaitForExit();
            }
        }
    }
}
