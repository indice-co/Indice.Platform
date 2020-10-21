using System.Data;

namespace Indice.Hosting
{
    /// <summary>
    /// 
    /// </summary>
    public interface IConnectionFactory
    {
        /// <summary>
        /// Creates a new <see cref="IDbConnection"/> implementation.
        /// </summary>
        IDbConnection Create();
    }
}
