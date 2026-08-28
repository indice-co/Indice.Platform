using Indice.Hosting.Data;
using Indice.Hosting.Data.Models;
using Indice.Hosting.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Indice.Hosting.Tests;

/// <summary>
/// Outbox tests with and without using the Outbox.
/// </summary>
public class OutboxIntegrationTests : IAsyncLifetime
{
    private readonly string _connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database=WorkerDb.Test_{Environment.Version.Major}_{Guid.NewGuid():N};Trusted_Connection=True;MultipleActiveResultSets=true";
    private readonly Guid _businessId = Guid.NewGuid();
    private readonly Guid _businessId2 = Guid.NewGuid();
    private const string QueueName = "test-event-queue";
    private const string QueueName2 = "test-event-queue2";
    
    private readonly IHost _host;
    private IServiceScope _scope = null!;
    private IntegratorDbContext _integratorDbContext = null!;
    private IMessageQueue<TestEvent> _queue = null!;
    private IMessageQueue<TestEvent2> _queue2 = null!;

    public OutboxIntegrationTests() {
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(configuration => {
                configuration.AddInMemoryCollection(new Dictionary<string, string?> {
                    ["ConnectionStrings:WorkerDb"] = _connectionString,
                    ["General:WorkerHostDisabled"] = "true"
                });
            })
            .ConfigureServices(services => {
                services.AddWorkerHost(options => {
                        options.WaitJobsToCompleteOnShutdown = true;
                        // use Outbox and register the integrator's DbContext as the TaskDbContext
                        options.UseStoreRelational<IntegratorDbContext>(builder => builder.UseSqlServer(_connectionString));
                    })
                    .AddJob<TestEventHandler>()
                    .WithQueueTrigger<TestEvent>(options => {
                        options.QueueName = QueueName;
                        options.PollingInterval = 500;
                        options.InstanceCount = 1;
                    })
                    .AddJob<TestEventHandler>()
                    .WithQueueTrigger<TestEvent2>(options => {
                        options.QueueName = QueueName2;
                        options.PollingInterval = 500;
                        options.InstanceCount = 1;
                    });
            })
            .Build();
    }

    #region No Outbox

    [Fact]
    public async Task EnqueueSingleSuccess() {
        await _queue.Enqueue(new TestEvent(_businessId));

        await AssertDatabaseData(_businessId, expectedEntities: 0, expectedEvents: 1);
        var message = await _queue.Dequeue();
        Assert.NotNull(message);
        await AssertDatabaseData(_businessId, expectedEntities: 0, expectedEvents: 0);
    }

    [Fact]
    public async Task EnqueueMultipleSameQueueSuccess() {
        await _queue.EnqueueRange([new TestEvent(_businessId), new TestEvent(_businessId2)]);

        var (entities, events) = await GetDatabaseEntities();

        Assert.Empty(entities);
        Assert.Equal(2, events.Count);
        Assert.Equal(1, events.Count(x => x.QueueName == QueueName && x.Payload.Contains(_businessId.ToString())));
        Assert.Equal(1, events.Count(x => x.QueueName == QueueName && x.Payload.Contains(_businessId2.ToString())));
    }
    
    /// <summary>
    /// Regression when publishing a message inside a transaction.
    /// Now the message is published transactionally.
    /// </summary>
    [Fact]
    public async Task OutboxSingleCallerOwnsTransaction() {
        await using (var transaction = await _integratorDbContext.Database.BeginTransactionAsync(TestContext.Current.CancellationToken)) {
            _integratorDbContext.Add(new TestEntity { Id = _businessId });
            await _queue.Enqueue(new TestEvent(_businessId));
            await _integratorDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            await transaction.CommitAsync(TestContext.Current.CancellationToken);
        }

        await AssertDatabaseData(_businessId, expectedEntities: 1, expectedEvents: 1);
    }
    
    /// <summary>
    /// Regression when publishing a message inside a transaction.
    /// Now the message is rolled back transactionally.
    /// </summary>
    [Fact]
    public async Task OutboxSingleCallerOwnsTransactionRollback() {
        await using (var transaction = await _integratorDbContext.Database.BeginTransactionAsync(TestContext.Current.CancellationToken)) {
            _integratorDbContext.Add(new TestEntity { Id = _businessId });
            await _queue.Enqueue(new TestEvent(_businessId));
            await _integratorDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            await transaction.RollbackAsync(TestContext.Current.CancellationToken);
        }

        await AssertDatabaseData(_businessId, expectedEntities: 0, expectedEvents: 0);
    }
    
    #endregion
    
    #region Outbox
    
    [Fact]
    public async Task OutboxSingleOk() {
        _integratorDbContext.Add(new TestEntity { Id = _businessId });
        _integratorDbContext.Enqueue(new TestEvent(_businessId));
        var count = await _integratorDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        
        Assert.Equal(2, count);
        await AssertDatabaseData(_businessId, expectedEntities: 1, expectedEvents: 1);
    }
    
    [Fact]
    public async Task AddAndEnqueueSingle() {
        _integratorDbContext.AddAndEnqueue(new TestEntity { Id = _businessId }, new TestEvent(_businessId));
        var count = await _integratorDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, count);
        await AssertDatabaseData(_businessId, expectedEntities: 1, expectedEvents: 1);
    }
    
    [Fact]
    public async Task OutboxSingleErrorInSaveChangesWritesNothing() {
        _integratorDbContext.Add(new TestEntity { Id = _businessId });
        _integratorDbContext.Enqueue(new TestEvent(_businessId));
        await Assert.ThrowsAsync<NotImplementedException>(() => _integratorDbContext.SaveChangesAsyncThrows(TestContext.Current.CancellationToken));

        await AssertDatabaseData(_businessId, expectedEntities: 0, expectedEvents: 0);
    }

    [Fact]
    public async Task AddAndEnqueueDuplicateThrowsDbUpdateException() {
        using (var scope = _host.Services.CreateScope()) {
            var integratorDbContext = scope.ServiceProvider.GetRequiredService<IntegratorDbContext>();
            integratorDbContext.TestEntities.Add(new TestEntity { Id = _businessId });
            await integratorDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        _integratorDbContext.AddAndEnqueue(new TestEntity { Id = _businessId }, new TestEvent(_businessId));

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => _integratorDbContext.SaveChangesAsync(TestContext.Current.CancellationToken));
        
        await AssertDatabaseData(_businessId, expectedEntities: 1, expectedEvents: 0);
    }

    [Fact]
    public async Task AddAndEnqueueTransactionRollsBack() {
        await using (var transaction = await _integratorDbContext.Database.BeginTransactionAsync(TestContext.Current.CancellationToken)) {
            _integratorDbContext.AddAndEnqueue(new TestEntity { Id = _businessId }, new TestEvent(_businessId));
            await _integratorDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            await transaction.RollbackAsync(TestContext.Current.CancellationToken);
        }

        await AssertDatabaseData(_businessId, expectedEntities: 0, expectedEvents: 0);
    }

    [Fact]
    public async Task AddAndEnqueueRange() {
        _integratorDbContext.AddAndEnqueueRange(
            [
                new TestEntity { Id = _businessId },
                new TestEntity { Id = _businessId2 }
            ],
            [
                new TestEvent(_businessId),
                new TestEvent(_businessId2)
            ]
        );

        Assert.Equal(4, await _integratorDbContext.SaveChangesAsync(TestContext.Current.CancellationToken));

        var (businessEntities, events) = await GetDatabaseEntities();
        Assert.Equal(2, businessEntities.Count);
        Assert.Equal(2, events.Count);
        Assert.All(events, x => Assert.Equal(QueueName, x.QueueName));
        Assert.Equal(1, events.Count(x => x.Payload.Contains(_businessId.ToString())));
        Assert.Equal(1, events.Count(x => x.Payload.Contains(_businessId2.ToString())));
    }
    
    [Fact]
    public async Task AddAndEnqueueRangeWithSecondQueueInOneSave() {
        _integratorDbContext.AddAndEnqueueRange([new TestEntity { Id = _businessId }], [new TestEvent(_businessId)]);
        _integratorDbContext.EnqueueRange([new TestEvent2(_businessId2)]);

        await _integratorDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (businessEntities, events) = await GetDatabaseEntities();
        Assert.Single(businessEntities);
        Assert.Equal(2, events.Count);
        Assert.Equal(1, events.Count(x => x.QueueName == QueueName && x.Payload.Contains(_businessId.ToString())));
        Assert.Equal(1, events.Count(x => x.QueueName == QueueName2 && x.Payload.Contains(_businessId2.ToString())));
    }

    [Fact]
    public async Task EnqueueDifferentQueuesInOneSave() {
        _integratorDbContext.Add(new TestEntity { Id = _businessId });
        _integratorDbContext.Enqueue(new TestEvent(_businessId));
        _integratorDbContext.Enqueue(new TestEvent2(_businessId2));

        await _integratorDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (businessEntities, events) = await GetDatabaseEntities();
        Assert.Single(businessEntities);
        Assert.Equal(2, events.Count);
        Assert.Equal(1, events.Count(x => x.QueueName == QueueName && x.Payload.Contains(_businessId.ToString())));
        Assert.Equal(1, events.Count(x => x.QueueName == QueueName2 && x.Payload.Contains(_businessId2.ToString())));
    }

    [Fact]
    public async Task EnqueueRangeOk() {
        _integratorDbContext.EnqueueRange([new TestEvent(_businessId), new TestEvent(_businessId2)]);

        await _integratorDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (_, events) = await GetDatabaseEntities();
        Assert.Equal(2, events.Count);
        Assert.All(events, x => Assert.Equal(QueueName, x.QueueName));
    }

    [Fact]
    public async Task AddAndEnqueueDelayedMessageNotVisible() {
        _integratorDbContext.AddAndEnqueue(new TestEntity { Id = _businessId }, new TestEvent(_businessId), DateTime.UtcNow.AddHours(1));
        await _integratorDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await AssertDatabaseData(_businessId, expectedEntities: 1, expectedEvents: 1);
        Assert.Null(await _queue.Dequeue());
    }

    [Fact]
    public async Task EnqueueVisibilityWindowNotVisible() {
        _integratorDbContext.Enqueue(new TestEvent(_businessId), TimeSpan.FromHours(1));
        _integratorDbContext.Enqueue(new TestEvent2(_businessId2));
        await _integratorDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Null(await _queue.Dequeue());
        Assert.NotNull(await _queue2.Dequeue());
    }

    #endregion

    private async Task<(List<TestEntity> entities, List<DbQMessage> events)> GetDatabaseEntities() {
        using var scope = _host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IntegratorDbContext>();
        var businessEntities = await dbContext.TestEntities.AsNoTracking().ToListAsync();
        var events = await dbContext.Set<DbQMessage>().AsNoTracking().ToListAsync();

        return (businessEntities, events);
    }

    private async Task AssertDatabaseData(Guid businessId, int expectedEntities, int expectedEvents) {
        var (businessEntities, events) = await GetDatabaseEntities();

        Assert.Equal(expectedEntities, businessEntities.Count);
        Assert.Equal(expectedEvents, events.Count);

        Assert.Equal(expectedEntities, businessEntities.Count(x => x.Id == businessId));
        Assert.Equal(expectedEvents, events.Count(x => x.QueueName == QueueName && x.Payload.Contains(businessId.ToString())));
    }

    public record TestEvent(Guid BusinessId);
    public record TestEvent2(Guid BusinessId);

    public class TestEventHandler
    {
        public Task Process(TestEvent @event) => Task.CompletedTask;
    }

    public class TestEntity
    {
        public Guid Id { get; set; }
    }

    public abstract class AnotherBaseDbContext : DbContext
    {
        protected AnotherBaseDbContext(DbContextOptions options) : base(options) { }
    }

    public class IntegratorDbContext : AnotherBaseDbContext, ITaskDbContext
    {
        public IntegratorDbContext(DbContextOptions<IntegratorDbContext> options) : base(options) { }

        public DbSet<TestEntity> TestEntities => Set<TestEntity>();

        public Task<int> SaveChangesAsyncThrows(CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }

        protected override void OnModelCreating(ModelBuilder builder) {
            base.OnModelCreating(builder);
            builder.ApplyWorkerConfiguration(providerName: Database.ProviderName);
        }
    }

    public async ValueTask InitializeAsync() {
        await _host.StartAsync();
        _scope = _host.Services.CreateScope();
        _integratorDbContext = _scope.ServiceProvider.GetRequiredService<IntegratorDbContext>();
        _queue = _scope.ServiceProvider.GetRequiredService<IMessageQueue<TestEvent>>();
        _queue2 = _scope.ServiceProvider.GetRequiredService<IMessageQueue<TestEvent2>>();

        await _integratorDbContext.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync() {
        _scope.Dispose();
        using (var scope = _host.Services.CreateScope()) {
            await scope.ServiceProvider.GetRequiredService<IntegratorDbContext>().Database.EnsureDeletedAsync();
        }
        await _host.StopAsync();
        _host.Dispose();
    }
}