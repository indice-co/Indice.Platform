using Indice.Extensions.Configuration.Database.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Indice.Extensions.Configuration.Database
{
    /// <summary>
    /// An Entity Framework Core based <see cref="ConfigurationProvider"/>.
    /// </summary>
    internal class EntityConfigurationProvider<TContext> : ConfigurationProvider, IDisposable where TContext : DbContext, IAppSettingsDbContext
    {
        private readonly EntityConfigurationOptions _options;
        private Task _pollingTask;
        private readonly CancellationTokenSource _cancellationToken;

        /// <summary>
        /// Creates a new instance of <see cref="EntityConfigurationProvider{T}"/>.
        /// </summary>
        /// <param name="options">Configuration options for <see cref="EntityConfigurationProvider{T}"/>.</param>
        public EntityConfigurationProvider(EntityConfigurationOptions options) {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _cancellationToken = new CancellationTokenSource();
            _pollingTask = null;
        }

        /// <inheritdoc/>
        public override void Load() => LoadData().ConfigureAwait(false).GetAwaiter().GetResult();

        /// <inheritdoc/>
        public void Dispose() => _cancellationToken.Cancel();

        /// <summary>
        /// Loads the configuration settings from the database.
        /// </summary>
        private async Task LoadData() {
            var builder = new DbContextOptionsBuilder<TContext>();
            _options.ConfigureDbContext?.Invoke(builder);
            using (var dbContext = (TContext)Activator.CreateInstance(typeof(TContext), new object[] { builder.Options })) {
                var canConnect = await dbContext.Database.CanConnectAsync();
                if (canConnect) {
                    var data = await dbContext.Set<AppSetting>().ToDictionaryAsync(x => x.Key, y => y.Value, StringComparer.OrdinalIgnoreCase);
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
