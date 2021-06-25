using System;
using System.Threading;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Indice.Extensions.Configuration
{
    /// <summary>
    /// An Entity Framework Core based <see cref="ConfigurationProvider"/>.
    /// </summary>
    internal class EntityConfigurationProvider : ConfigurationProvider, IDisposable
    {
        private event EventHandler AppSettingsChanged;
        private readonly EntityConfigurationOptions _options;
        private Task _pollingTask;
        private readonly CancellationTokenSource _cancellationToken;

        /// <summary>
        /// Creates a new instance of <see cref="EntityConfigurationProvider"/>.
        /// </summary>
        /// <param name="options">Configuration options for <see cref="EntityConfigurationProvider"/>.</param>
        public EntityConfigurationProvider(EntityConfigurationOptions options) {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _cancellationToken = new CancellationTokenSource();
            _pollingTask = null;
            AppSettingsChanged += EntityConfigurationProviderAppSettingsChanged;
        }

        /// <inheritdoc/>
        public override void Load() => LoadData().ConfigureAwait(false).GetAwaiter().GetResult();

        internal virtual void OnAppSettingsChanged() {
            var handler = AppSettingsChanged;
            handler?.Invoke(this, null);
        }

        /// <inheritdoc/>
        public void Dispose() => _cancellationToken.Cancel();

        private void EntityConfigurationProviderAppSettingsChanged(object sender, EventArgs eventArgs) {
            if (_options.ReloadOnDatabaseChange) {
                Load();
            }
        }

        /// <summary>
        /// Loads the configuration settings from the database.
        /// </summary>
        private async Task LoadData() {
            var builder = new DbContextOptionsBuilder<IdentityDbContext>();
            _options.ConfigureDbContext?.Invoke(builder);
            using (var dbContext = new IdentityDbContext(builder.Options)) {
                var canConnect = await dbContext.Database.CanConnectAsync();
                if (canConnect) {
                    var data = await dbContext.AppSettings.ToDictionaryAsync(x => x.Key, y => y.Value, StringComparer.OrdinalIgnoreCase);
                    if (data != null) {
                        Data = data;
                    }
                }
            }
            OnReload();
            // Schedule a polling task only if none exists and a valid delay is specified.
            if (_pollingTask == null && _options.ReloadOnInterval.HasValue) {
                _pollingTask = PollForSettingsChanges();
            }
        }

        private async Task WaitForReload() => await Task.Delay(_options.ReloadOnInterval.Value, _cancellationToken.Token);

        private async Task PollForSettingsChanges() {
            while (!_cancellationToken.IsCancellationRequested) {
                await WaitForReload();
                try {
                    await LoadData();
                } catch { }
            }
        }
    }
}
