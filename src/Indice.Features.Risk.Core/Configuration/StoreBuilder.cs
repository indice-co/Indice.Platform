using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Risk.Core.Configuration;

/// <summary>Builder class used to configure the underlying store of the transactions.</summary>
/// <typeparam name="TTransaction">The type of transaction that the engine manages.</typeparam>
public class StoreBuilder<TTransaction> where TTransaction : Transaction
{
    internal StoreBuilder(IServiceCollection services) {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    internal IServiceCollection Services { get; }
    internal List<(string Name, Type RuleTypes)> NamedRulesCollection { get; } = new List<(string Name, Type RuleTypes)>();

    /// <summary>Registers an implementation of <see cref="ITransactionStore{TTransaction}"/> and <see cref="IEventStore"/> where Entity Framework Core is used as a persistent mechanism.</summary>
    /// <typeparam name="TContext">The type of <see cref="DbContext"/>.</typeparam>
    /// <param name="dbContextOptionsAction">The builder being used to configure the context.</param>
    /// <returns>An instance of <see cref="RuleBuilder{TTransaction}"/> for further configuration.</returns>
    public RuleBuilder<TTransaction> WithEntityFrameworkCoreStores<TContext>(Action<DbContextOptionsBuilder> dbContextOptionsAction) where TContext : DbContext {
        Services.AddDbContext<TContext>(dbContextOptionsAction);
        Services.AddTransient<ITransactionStore<TTransaction>, TransactionStoreEntityFrameworkCore<TTransaction>>();
        Services.AddTransient<IEventStore, EventStoreEntityFrameworkCore<TTransaction>>();
        return new RuleBuilder<TTransaction>(Services);
    }

    /// <summary>Registers an implementation of <see cref="ITransactionStore{TTransaction}"/> and <see cref="IEventStore"/> where Entity Framework Core is used as a persistent mechanism.</summary>
    /// <param name="dbContextOptionsAction">The builder being used to configure the context.</param>
    /// <returns>An instance of <see cref="RuleBuilder{TTransaction}"/> for further configuration.</returns>
    public RuleBuilder<TTransaction> WithEntityFrameworkCoreStores(Action<DbContextOptionsBuilder> dbContextOptionsAction) =>
        WithEntityFrameworkCoreStores<RiskDbContext<TTransaction>>(dbContextOptionsAction);
}
