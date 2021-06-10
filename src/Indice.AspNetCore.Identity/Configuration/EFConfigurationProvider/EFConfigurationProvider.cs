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
    internal class EFConfigurationProvider : ConfigurationProvider, IDisposable
    {
        private readonly Action<DbContextOptionsBuilder> _configureAction;
        private readonly TimeSpan? _reloadInterval;
        private Task _pollingTask;
        private readonly CancellationTokenSource _cancellationToken;

        /// <summary>
        /// Creates a new instance of <see cref="EFConfigurationProvider"/>.
        /// </summary>
        /// <param name="configureAction">The <see cref="DbContextOptions"/> to use.</param>
        /// <param name="reloadInterval">The <see cref="TimeSpan"/> to wait in between each attempt at polling the database for changes. Default is null which indicates no reloading.</param>
        public EFConfigurationProvider(Action<DbContextOptionsBuilder> configureAction, TimeSpan? reloadInterval = null) {
            if (reloadInterval.HasValue && reloadInterval.Value <= TimeSpan.Zero) {
                throw new ArgumentOutOfRangeException(nameof(reloadInterval), reloadInterval, $"Parameter {nameof(reloadInterval)} must have a positive value.");
            }
            _configureAction = configureAction ?? throw new ArgumentNullException(nameof(configureAction));
            _reloadInterval = reloadInterval;
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
            var builder = new DbContextOptionsBuilder<IdentityDbContext>();
            _configureAction(builder);
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
            if (_pollingTask == null && _reloadInterval != null) {
                _pollingTask = PollForSettingsChanges();
            }
        }

        private async Task WaitForReload() => await Task.Delay(_reloadInterval.Value, _cancellationToken.Token);

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
