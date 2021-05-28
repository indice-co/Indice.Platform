using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Indice.Serialization;
using Indice.Types;
using Xunit;

namespace Indice.Services.Tests
{
    public class LockManagerAzureTests
    {
        private readonly string _connectionString = "UseDevelopmentStorage=true;DevelopmentStorageProxyUri=http://127.0.0.1";
        private readonly ILockManager _LockManager;
        private readonly IFileService _FileService;
        public LockManagerAzureTests() {
            if (_connectionString.StartsWith("UseDevelopmentStorage=true;")) { 
                StorageEmulator.Start();
            }
            _LockManager = new LockManagerAzure(new LockManagerAzureOptions {
                EnvironmentName = "test",
                StorageConnection = _connectionString
            });
            _FileService = new FileServiceAzureStorage(_connectionString, "test");
        }
        [Fact]
        public async Task AquireLockTest() {   
            var duration = TimeSpan.FromSeconds(15);
            var name = "constantinos"; // using a random name :)
            var @lock = await _LockManager.AcquireLock(name, duration);
            await using (@lock) {
                await Task.Delay(TimeSpan.FromSeconds(0.5));
            }
            var @lock2 = await _LockManager.AcquireLock(name, duration);
            await using (@lock2) {
                await Task.Delay(TimeSpan.FromSeconds(0.5));
            }
            var result = await _LockManager.TryAcquireLock(name);
            if (result.Ok) { 
                await using (result.Lock) {
                    await Task.Delay(TimeSpan.FromSeconds(0.5));
                }
            }
        }


        [Fact(Skip = "Only for debug purposes")]
        public async Task FunctionLockingTestMaster() {
            var duration = TimeSpan.FromSeconds(60);
            var operation = "MasterProductImport"; // using a random name :)
            var @lock = await _LockManager.AcquireLock(operation, duration);
            await _FileService.SaveAsync($"messages/{operation}.json", Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new Tuple<string, string>(@lock.LeaseId, @lock.Name))));
        }

        [Fact(Skip = "Only for debug purposes")]
        public async Task FunctionLockingTestDetail() {
            var operation = "MasterProductImport"; // using a random name :)
            var bytes = await _FileService.GetAsync($"messages/{operation}.json");
            var message = JsonSerializer.Deserialize<(string LeaseId, string Name)>(Encoding.UTF8.GetString(bytes), JsonSerializerOptionDefaults.GetDefaultSettings());
            var @lock = await _LockManager.Renew(message.Name, message.LeaseId);
            await Task.Delay(TimeSpan.FromSeconds(10));
        }
    }
}
