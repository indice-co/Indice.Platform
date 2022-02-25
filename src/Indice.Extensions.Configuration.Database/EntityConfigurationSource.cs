using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Indice.Extensions.Configuration.Database
{
    /// <summary>
    /// Represents database secrets as an <see cref="IConfigurationSource"/>.
    /// </summary>
    internal class EntityConfigurationSource<TContext> : IConfigurationSource where TContext : DbContext, IAppSettingsDbContext
    {
        private readonly EntityConfigurationOptions _options;

        /// <summary>
        /// Creates a new instance of <see cref="EntityConfigurationSource{T}"/>.
        /// </summary>
        /// <param name="options">Configuration options for <see cref="EntityConfigurationProvider{T}"/>.</param>
        public EntityConfigurationSource(EntityConfigurationOptions options) {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Builds the <see cref="IConfigurationProvider"/> for this source.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        public IConfigurationProvider Build(IConfigurationBuilder builder) => new EntityConfigurationProvider<TContext>(_options);
    }
}
