#if NET8_0
using AutoMapper;
using Elsa.Models;
using Elsa.Options;
using Elsa.Persistence.EntityFramework.Core;
using Elsa.Persistence.EntityFramework.Core.Services;
using Elsa.Persistence.EntityFramework.Core.Stores;
using Elsa.Persistence.Specifications;
using Elsa.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Cases.Workflows;
internal class ElsaStores
{
    public abstract class ElsaContextEntityFrameworkStore<T> : EntityFrameworkStore<T, ElsaContext> where T : class, IEntity
    {
        protected ElsaContextEntityFrameworkStore(IContextFactory<ElsaContext> dbContextFactory, IMapper mapper, ILogger logger) : base(dbContextFactory, mapper, logger) {
        }
        public override async Task<int> DeleteManyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default) {
            var filter = MapSpecification(specification);
            return await DoWork(async dbContext => {
                return await dbContext.Set<T>().Where(filter).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

    }

}

internal static class ElsaStoresExtensions
{
    public static ElsaOptionsBuilder AddElsaStores(this ElsaOptionsBuilder elsa) {
        elsa.Services
            .AddScoped<EntityFrameworkBookmarkStore, EntityFrameworkBookmarkStoreCompat>()
            .AddScoped<EntityFrameworkWorkflowDefinitionStore, EntityFrameworkWorkflowDefinitionStoreCompat>()
            .AddScoped<EntityFrameworkTriggerStore, EntityFrameworkTriggerStoreCompat>()
            .AddScoped<EntityFrameworkWorkflowInstanceStore, EntityFrameworkWorkflowInstanceStoreCompat>()
            .AddScoped<EntityFrameworkWorkflowExecutionLogRecordStore, EntityFrameworkWorkflowExecutionLogRecordStoreCompat>();
        return elsa;
    }

    internal class EntityFrameworkBookmarkStoreCompat : EntityFrameworkBookmarkStore
    {
        public EntityFrameworkBookmarkStoreCompat(IElsaContextFactory dbContextFactory, IMapper mapper, ILogger<EntityFrameworkBookmarkStoreCompat> logger)
            : base(dbContextFactory, mapper, logger) {
        }

        public override async Task<int> DeleteManyAsync(ISpecification<Bookmark> specification, CancellationToken cancellationToken = default) {
            var filter = MapSpecification(specification);
            return await DoWork(async dbContext => {
                return await dbContext.Bookmarks.Where(filter).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }
    }
    internal class EntityFrameworkTriggerStoreCompat : EntityFrameworkTriggerStore
    {
        public EntityFrameworkTriggerStoreCompat(IElsaContextFactory dbContextFactory, IMapper mapper, ILogger<EntityFrameworkTriggerStoreCompat> logger)
            : base(dbContextFactory, mapper, logger) {
        }

        public override async Task<int> DeleteManyAsync(ISpecification<Trigger> specification, CancellationToken cancellationToken = default) {
            var filter = MapSpecification(specification);
            return await DoWork(async dbContext => {
                return await dbContext.Triggers.Where(filter).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }
    }
    internal class EntityFrameworkWorkflowDefinitionStoreCompat : EntityFrameworkWorkflowDefinitionStore
    {
        public EntityFrameworkWorkflowDefinitionStoreCompat(IElsaContextFactory dbContextFactory, IMapper mapper, IContentSerializer contentSerializer, ILogger<EntityFrameworkWorkflowDefinitionStoreCompat> logger)
            : base(dbContextFactory, mapper, contentSerializer, logger) {
        }

        public override async Task<int> DeleteManyAsync(ISpecification<WorkflowDefinition> specification, CancellationToken cancellationToken = default) {
            var filter = MapSpecification(specification);
            return await DoWork(async dbContext => {
                return await dbContext.WorkflowDefinitions.Where(filter).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }
    }
    internal class EntityFrameworkWorkflowInstanceStoreCompat : EntityFrameworkWorkflowInstanceStore
    {
        public EntityFrameworkWorkflowInstanceStoreCompat(IElsaContextFactory dbContextFactory, IMapper mapper, IContentSerializer contentSerializer, ILogger<EntityFrameworkWorkflowInstanceStoreCompat> logger)
            : base(dbContextFactory, mapper, contentSerializer, logger) {
        }
        public override async Task<int> DeleteManyAsync(ISpecification<WorkflowInstance> specification, CancellationToken cancellationToken = default) {
            var filter = MapSpecification(specification);
            return await DoWork(async dbContext => {
                return await dbContext.WorkflowInstances.Where(filter).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }
    }
    internal class EntityFrameworkWorkflowExecutionLogRecordStoreCompat : EntityFrameworkWorkflowExecutionLogRecordStore
    {
        public EntityFrameworkWorkflowExecutionLogRecordStoreCompat(IElsaContextFactory dbContextFactory, IMapper mapper, ILogger<EntityFrameworkWorkflowExecutionLogRecordStoreCompat> logger)
            : base(dbContextFactory, mapper, logger) {
        }
        public override async Task<int> DeleteManyAsync(ISpecification<WorkflowExecutionLogRecord> specification, CancellationToken cancellationToken = default) {
            var filter = MapSpecification(specification);
            return await DoWork(async dbContext => {
                return await dbContext.WorkflowExecutionLogRecords.Where(filter).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }
    }
}
#endif