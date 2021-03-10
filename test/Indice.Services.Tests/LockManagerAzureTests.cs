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
    public class LockManagerAzureTests
    {
        private readonly string _connectionString = "UseDevelopmentStorage=true;DevelopmentStorageProxyUri=http://127.0.0.1";
        private readonly ILockManager _LockManager;
        public LockManagerAzureTests() {
            if (_connectionString.StartsWith("UseDevelopmentStorage=true;")) { 
                StorageEmulator.Start();
            }
            _LockManager = new LockManagerAzure(new LockManagerAzureOptions {
                EnvironmentName = "test",
                StorageConnection = _connectionString
            });
        }
        [Fact]
        public async Task AquireLockTest() {
            
            var duration = TimeSpan.FromSeconds(15);
            var name = "constantinos"; // using a random name :)
            var @lock = _LockManager.AcquireLock(name, duration);
            using (@lock) {
                await Task.Delay(TimeSpan.FromSeconds(0.5));
            }
            var @lock2 = _LockManager.AcquireLock(name, duration);
            using (@lock2) {
                await Task.Delay(TimeSpan.FromSeconds(0.5));
            }
        }
    }
}
