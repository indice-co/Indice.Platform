using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Risk.Core.Configuration;

/// <summary>Builder class used to configure the underlying store of the risk events.</summary>
/// <typeparam name="TRiskEvent">The type of risk event.</typeparam>
public class StoreBuilder<TRiskEvent> where TRiskEvent : DbRiskEvent
{
    internal StoreBuilder(IServiceCollection services) {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    internal IServiceCollection Services { get; }

    /// <summary>Registers an implementation of <see cref="IRiskEventStore{TRiskEvent}"/> where Entity Framework Core is used as a persistent mechanism.</summary>
    /// <typeparam name="TContext">The type of <see cref="DbContext"/>.</typeparam>
    /// <param name="dbContextOptionsAction">The builder being used to configure the context.</param>
    /// <returns>An instance of <see cref="RuleBuilder{TRiskEvent}"/> for further configuration.</returns>
    public RuleBuilder<TRiskEvent> WithEntityFrameworkCoreStore<TContext>(Action<DbContextOptionsBuilder> dbContextOptionsAction) where TContext : DbContext {
        Services.AddDbContext<TContext>(dbContextOptionsAction);
        Services.AddTransient<IRiskEventStore<TRiskEvent>, RiskEventStoreEntityFrameworkCore<TRiskEvent>>();
        return new RuleBuilder<TRiskEvent>(Services);
    }

    /// <summary>Registers an implementation of <see cref="IRiskEventStore{TRiskEvent}"/> where Entity Framework Core is used as a persistent mechanism.</summary>
    /// <param name="dbContextOptionsAction">The builder being used to configure the context.</param>
    /// <returns>An instance of <see cref="RuleBuilder{TRiskEvent}"/> for further configuration.</returns>
    public RuleBuilder<TRiskEvent> WithEntityFrameworkCoreStore(Action<DbContextOptionsBuilder> dbContextOptionsAction) =>
        WithEntityFrameworkCoreStore<RiskDbContext<TRiskEvent>>(dbContextOptionsAction);
}
