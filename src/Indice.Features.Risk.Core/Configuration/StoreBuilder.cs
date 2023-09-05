using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data;
using Indice.Features.Risk.Core.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Risk.Core.Configuration;

/// <summary>Builder class used to configure the underlying store of the risk events.</summary>
public class StoreBuilder
{
    internal StoreBuilder(IServiceCollection services) {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    internal IServiceCollection Services { get; }

    /// <summary>Registers an implementation of <see cref="IRiskEventStore"/> where Entity Framework Core is used as a persistent mechanism.</summary>
    /// <param name="dbContextOptionsAction">The builder being used to configure the context.</param>
    /// <returns>An instance of <see cref="RiskRuleBuilder"/> for further configuration.</returns>
    public RiskRuleBuilder WithEntityFrameworkCoreStore(Action<DbContextOptionsBuilder> dbContextOptionsAction) {
        Services.AddDbContext<RiskDbContext>(dbContextOptionsAction);
        Services.AddTransient<IRiskEventStore, RiskEventStoreEntityFrameworkCore>();
        return new RiskRuleBuilder(Services);
    }
}
