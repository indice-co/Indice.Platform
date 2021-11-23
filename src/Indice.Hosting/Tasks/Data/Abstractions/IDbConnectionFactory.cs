using System.Data;

namespace Indice.Hosting.Data
{
    /// <summary>
    /// A factory class that generates instances of type <see cref="IDbConnection"/>.
    /// </summary>
    public interface IDbConnectionFactory
    {
        /// <summary>
        /// Constructs a new instance of type <see cref="IDbConnection"/>.
        /// </summary>
        IDbConnection Create();
    }
}
