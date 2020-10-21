using System;
using System.Data;

namespace Indice.Hosting
{
    /// <summary>
    /// The base class for all classes that use a database connection. Used to handle disposal of underlying resources.
    /// </summary>
    /// <remarks>https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose</remarks>
    public abstract class RepositoryBase : IDisposable
    {
        // To detect redundant calls.
        private bool _disposed;

        /// <summary>
        /// 
        /// </summary>
        protected IDbConnection Connection { get; }

        public RepositoryBase(IConnectionFactory connectionFactory) {
            Connection = connectionFactory.Create();
        }

        /// <summary>
        /// Public implementation of Dispose pattern callable by consumers.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void OnDispose() { 
        
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing">To detect redundant calls.</param>
        protected void Dispose(bool disposing) {
            if (_disposed) {
                return;
            }
            if (disposing) {
                Connection.Dispose();
                // TODO: dispose managed state (managed objects).
                OnDispose();
            }
            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.
            _disposed = true;
        }

        /// <summary>
        /// Class destructor.
        /// </summary>
        ~RepositoryBase() => Dispose(false);
    }
}
