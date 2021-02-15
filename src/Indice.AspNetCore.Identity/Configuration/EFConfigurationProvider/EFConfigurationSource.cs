using System;
using Microsoft.Extensions.Configuration;

namespace Indice.Extensions.Configuration.EntityFrameworkCore
{
    /// <summary>
    /// Represents database secrets as an <see cref="IConfigurationSource"/>.
    /// </summary>
    internal class EFConfigurationSource : IConfigurationSource
    {
        private readonly Action<EFConfigurationOptions> _configureAction;

        /// <summary>
        /// Creates a new instance of <see cref="EFConfigurationSource"/>.
        /// </summary>
        /// <param name="configureAction">The <see cref="EFConfigurationOptions"/> to use.</param>
        public EFConfigurationSource(Action<EFConfigurationOptions> configureAction) {
            _configureAction = configureAction ?? throw new ArgumentNullException(nameof(configureAction));
        }

        /// <summary>
        /// Builds the <see cref="IConfigurationProvider"/> for this source.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        public IConfigurationProvider Build(IConfigurationBuilder builder) {
            var options = new EFConfigurationOptions();
            _configureAction(options);
            return new EFConfigurationProvider(options.DbContextOptionsBuilder, options.ReloadInterval);
        }
    }
}
