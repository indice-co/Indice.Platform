using System;
using System.ServiceProcess;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting
{
    internal class GenericServiceHost : ServiceBase
    {
        private IHost _host;
        private bool _stopRequestedByWindows;

        public GenericServiceHost(IHost host) {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        protected sealed override void OnStart(string[] args) {
            OnStarting(args);

            _host
                .Services
                .GetRequiredService<IHostApplicationLifetime>()
                .ApplicationStopped
                .Register(() => {
                    if (!_stopRequestedByWindows) {
                        Stop();
                    }
                });

            _host.Start();

            OnStarted();
        }

        protected sealed override void OnStop() {
            _stopRequestedByWindows = true;
            OnStopping();
            try {
                _host.StopAsync().GetAwaiter().GetResult();
            } finally {
                _host.Dispose();
                OnStopped();
            }
        }

        protected virtual void OnStarting(string[] args) { }

        protected virtual void OnStarted() { }

        protected virtual void OnStopping() { }

        protected virtual void OnStopped() { }
    }
}
