using Indice.Hosting.Data;
using Indice.Hosting.Data.Models;
using Indice.Hosting.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Indice.Hosting.Tests;

public class OutboxIntegrationTests : IAsyncLifetime
{
    private readonly string _connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database=WorkerDb.Test_{Environment.Version.Major}_{Guid.NewGuid():N};Trusted_Connection=True;MultipleActiveResultSets=true";
    private readonly Guid _businessId = Guid.NewGuid();
    private readonly Guid _businessId2 = Guid.NewGuid();
    private const string QueueName = "test-event-queue";
    private const string QueueName2 = "test-event-queue2";
    
    private readonly IHost _host;
    private IServiceScope _scope = null!;
    private TestDbContext _testDbContext = null!;
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
                        options.UseStoreRelational<TestDbContext>(builder => builder.UseSqlServer(_connectionString));
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

    #region Regression

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
    #endregion
    
    #region Outbox

    [Fact]
    public async Task OutboxSingleCallerOwnsTransaction() {
        await using (var transaction = await _testDbContext.Database.BeginTransactionAsync()) {
            await _queue.Enqueue(new TestEvent(_businessId));
            await transaction.CommitAsync();
        }

        await AssertDatabaseData(_businessId, expectedEntities: 0, expectedEvents: 1);
    }

    // todo: changed behaviour
    [Fact]
    public async Task OutboxSingleCallerOwnsTransactionRollsBack() {
        await using (var transaction = await _testDbContext.Database.BeginTransactionAsync()) {
            await _queue.Enqueue(new TestEvent(_businessId));
            await transaction.RollbackAsync();
        }

        await AssertDatabaseData(_businessId, expectedEntities: 0, expectedEvents: 0);
    }
    
    [Fact]
    public async Task AddAndEnqueueSingle() {
        _testDbContext.AddAndEnqueue(new TestEntity { Id = _businessId }, new TestEvent(_businessId));

        Assert.Equal(2, await _testDbContext.SaveChangesAsync());
        await AssertDatabaseData(_businessId, expectedEntities: 1, expectedEvents: 1);
    }

    [Fact]
    public async Task AddAndEnqueueNothingIsWrittenBeforeSaveChanges() {
        _testDbContext.AddAndEnqueue(new TestEntity { Id = _businessId }, new TestEvent(_businessId));

        await AssertDatabaseData(_businessId, expectedEntities: 0, expectedEvents: 0);
    }

    [Fact]
    public async Task AddAndEnqueueDuplicateThrowsDbUpdateException() {
        using (var scope = _host.Services.CreateScope()) {
            var testDbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            testDbContext.TestEntities.Add(new TestEntity { Id = _businessId });
            await testDbContext.SaveChangesAsync();
        }

        _testDbContext.AddAndEnqueue(new TestEntity { Id = _businessId }, new TestEvent(_businessId));

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => _testDbContext.SaveChangesAsync());
        
        await AssertDatabaseData(_businessId, expectedEntities: 1, expectedEvents: 0);
    }

    [Fact]
    public async Task AddAndEnqueueTransactionRollsBack() {
        await using (var transaction = await _testDbContext.Database.BeginTransactionAsync()) {
            _testDbContext.AddAndEnqueue(new TestEntity { Id = _businessId }, new TestEvent(_businessId));
            await _testDbContext.SaveChangesAsync();
            await transaction.RollbackAsync();
        }

        await AssertDatabaseData(_businessId, expectedEntities: 0, expectedEvents: 0);
    }

    [Fact]
    public async Task AddAndEnqueueRangeIsAtomic() {
        _testDbContext.AddAndEnqueueRange(
            [
                new TestEntity { Id = _businessId },
                new TestEntity { Id = _businessId2 }
            ],
            [
                new TestEvent(_businessId),
                new TestEvent(_businessId2)
            ]
        );

        Assert.Equal(4, await _testDbContext.SaveChangesAsync());

        var (businessEntities, events) = await GetDatabaseEntities();
        Assert.Equal(2, businessEntities.Count);
        Assert.Equal(2, events.Count);
        Assert.All(events, x => Assert.Equal(QueueName, x.QueueName));
        Assert.Equal(1, events.Count(x => x.Payload.Contains(_businessId.ToString())));
        Assert.Equal(1, events.Count(x => x.Payload.Contains(_businessId2.ToString())));
    }
    
    [Fact]
    public async Task AddAndEnqueueRangeWithSecondQueueInOneSave() {
        _testDbContext.AddAndEnqueueRange([new TestEntity { Id = _businessId }], [new TestEvent(_businessId)]);
        _testDbContext.EnqueueRange([new TestEvent2(_businessId2)]);

        await _testDbContext.SaveChangesAsync();

        var (businessEntities, events) = await GetDatabaseEntities();
        Assert.Single(businessEntities);
        Assert.Equal(2, events.Count);
        Assert.Equal(1, events.Count(x => x.QueueName == QueueName && x.Payload.Contains(_businessId.ToString())));
        Assert.Equal(1, events.Count(x => x.QueueName == QueueName2 && x.Payload.Contains(_businessId2.ToString())));
    }

    [Fact]
    public async Task EnqueueDifferentQueuesInOneSave() {
        _testDbContext.Add(new TestEntity { Id = _businessId });
        _testDbContext.Enqueue(new TestEvent(_businessId));
        _testDbContext.Enqueue(new TestEvent2(_businessId2));

        await _testDbContext.SaveChangesAsync();

        var (businessEntities, events) = await GetDatabaseEntities();
        Assert.Single(businessEntities);
        Assert.Equal(2, events.Count);
        Assert.Equal(1, events.Count(x => x.QueueName == QueueName && x.Payload.Contains(_businessId.ToString())));
        Assert.Equal(1, events.Count(x => x.QueueName == QueueName2 && x.Payload.Contains(_businessId2.ToString())));
    }

    [Fact]
    public async Task EnqueueRangeOk() {
        _testDbContext.EnqueueRange([new TestEvent(_businessId), new TestEvent(_businessId2)]);

        await _testDbContext.SaveChangesAsync();

        var (_, events) = await GetDatabaseEntities();
        Assert.Equal(2, events.Count);
        Assert.All(events, x => Assert.Equal(QueueName, x.QueueName));
    }

    [Fact]
    public async Task AddAndEnqueueDelayedMessageNotVisible() {
        _testDbContext.AddAndEnqueue(new TestEntity { Id = _businessId }, new TestEvent(_businessId), DateTime.UtcNow.AddHours(1));
        await _testDbContext.SaveChangesAsync();

        await AssertDatabaseData(_businessId, expectedEntities: 1, expectedEvents: 1);
        Assert.Null(await _queue.Dequeue());
    }

    [Fact]
    public async Task EnqueueVisibilityWindowNotVisible() {
        _testDbContext.Enqueue(new TestEvent(_businessId), TimeSpan.FromHours(1));
        _testDbContext.Enqueue(new TestEvent2(_businessId2));
        await _testDbContext.SaveChangesAsync();

        Assert.Null(await _queue.Dequeue());
        Assert.NotNull(await _queue2.Dequeue());
    }

    #endregion

    private async Task<(List<TestEntity> entities, List<DbQMessage> events)> GetDatabaseEntities() {
        using var scope = _host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var businessEntities = await dbContext.TestEntities.AsNoTracking().ToListAsync();
        var events = await dbContext.Queue.AsNoTracking().ToListAsync();

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

    /// <summary>The integrator's context.</summary>
    public class TestDbContext : TaskDbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<TestEntity> TestEntities => Set<TestEntity>();
    }

    public async Task InitializeAsync() {
        await _host.StartAsync();
        _scope = _host.Services.CreateScope();
        _testDbContext = _scope.ServiceProvider.GetRequiredService<TestDbContext>();
        _queue = _scope.ServiceProvider.GetRequiredService<IMessageQueue<TestEvent>>();
        _queue2 = _scope.ServiceProvider.GetRequiredService<IMessageQueue<TestEvent2>>();

        await _testDbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() {
        _scope.Dispose();
        using (var scope = _host.Services.CreateScope()) {
            await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureDeletedAsync();
        }
        await _host.StopAsync();
        _host.Dispose();
    }
}