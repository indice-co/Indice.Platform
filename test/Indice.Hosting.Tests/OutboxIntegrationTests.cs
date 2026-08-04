using Indice.Hosting.Data;
using Indice.Hosting.Data.Models;
using Indice.Hosting.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
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
                services.AddDbContext<TestDbContext>(options => options.UseSqlServer(_connectionString));
                services.AddWorkerHost(options => {
                        options.WaitJobsToCompleteOnShutdown = true;
                        options.UseStoreRelational();
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
        await _testDbContext.SaveAndEnqueueAsync(_queue, new TestEvent(_businessId));
        
        await AssertDatabaseData(_businessId, expectedEntities: 0, expectedEvents: 1);
        var message = await _queue.Dequeue();
        Assert.NotNull(message);
        await AssertDatabaseData(_businessId, expectedEntities: 0, expectedEvents: 0);
    }
    
    [Fact]
    public async Task EnqueueMultipleSameQueueSuccess() {
        await _queue.EnqueueRange([new TestEvent(_businessId), new TestEvent(_businessId2)]);
        
        using var scope = _host.Services.CreateScope();
        var testDbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var taskDbContext = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
        var entities = await testDbContext.TestEntities.ToListAsync();
        var events = await taskDbContext.Queue.ToListAsync();
        
        Assert.Empty(entities);
        Assert.Equal(2, events.Count);
        
        Assert.Equal(1, events.Count(x => x.QueueName == QueueName && x.Payload.Contains(_businessId.ToString())));
        Assert.Equal(1, events.Count(x => x.QueueName == QueueName && x.Payload.Contains(_businessId2.ToString())));
    }
    
    #endregion

    #region Outbox
    
    [Fact]
    public async Task OutboxSingle() {
        _testDbContext.Add(new TestEntity { Id = _businessId });
        var affected = await _testDbContext.SaveAndEnqueueAsync(_queue, new TestEvent(_businessId));

        Assert.Equal(1, affected);
        await AssertDatabaseData(_businessId, expectedEntities: 1, expectedEvents: 1);
    }
    
    [Fact]
    public async Task OutboxSingleCallerOwnsTransaction() {
        await using (var transaction = await _testDbContext.Database.BeginTransactionAsync()) {
            _testDbContext.TestEntities.Add(new TestEntity { Id = _businessId });
            await _testDbContext.SaveAndEnqueueAsync(_queue, new TestEvent(_businessId));
            await transaction.CommitAsync();
        }

        await AssertDatabaseData(_businessId, expectedEntities: 1, expectedEvents: 1);
    }
    
    [Fact]
    public async Task OutboxMultipleCallerOwnsTransaction() {
        await using (var transaction = await _testDbContext.Database.BeginTransactionAsync()) {
            _testDbContext.TestEntities.Add(new TestEntity { Id = _businessId });
            await _testDbContext.SaveAndEnqueueAsync(_queue, new TestEvent(_businessId));
            await _testDbContext.SaveAndEnqueueAsync(_queue, new TestEvent(_businessId));
            await transaction.CommitAsync();
        }

        await AssertDatabaseData(_businessId, expectedEntities: 1, expectedEvents: 2);
    }
    
    [Fact]
    public async Task OutboxMultipleBatchDifferentQueues() {
        _testDbContext.Add(new TestEntity { Id = _businessId });
        await _testDbContext.SaveAndEnqueueAsync(batch => batch
            .Add(_queue, new TestEvent(_businessId))
            .Add(_queue2, new TestEvent2(_businessId2))
        );
        
        var (businessEntities, events) = await GetDatabaseEntities();
        Assert.Single(businessEntities);
        Assert.Equal(2, events.Count);
        
        Assert.Equal(1, events.Count(x => x.QueueName == QueueName && x.Payload.Contains(_businessId.ToString())));
        Assert.Equal(1, events.Count(x => x.QueueName == QueueName2 && x.Payload.Contains(_businessId2.ToString())));
    }

    [Fact]
    public async Task OutboxSingleCallerOwnsTransactionRollback() {
        await using (var transaction = await _testDbContext.Database.BeginTransactionAsync()) {
            _testDbContext.TestEntities.Add(new TestEntity { Id = _businessId });
            await _testDbContext.SaveAndEnqueueAsync(_queue, new TestEvent(_businessId));
            await transaction.RollbackAsync();
        }

        await AssertDatabaseData(_businessId, expectedEntities: 0, expectedEvents: 0);
    }

    [Fact]
    public async Task OutboxSingleDbUpdateExceptionNoEventProduced() {
        using (var scope = _host.Services.CreateScope()) {
            var seedDbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            seedDbContext.TestEntities.Add(new TestEntity { Id = _businessId });
            await seedDbContext.SaveChangesAsync();
        }

        _testDbContext.TestEntities.Add(new TestEntity { Id = _businessId });
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => _testDbContext.SaveAndEnqueueAsync(_queue, new TestEvent(_businessId)));
        await AssertDatabaseData(_businessId, expectedEntities: 1, expectedEvents: 0);
    }

    [Fact]
    public async Task OutboxSingleDelayedMessageNotVisible() {
        await _testDbContext.SaveAndEnqueueAsync(_queue, new TestEvent(_businessId), DateTime.UtcNow.AddHours(1));

        await AssertDatabaseData(_businessId, expectedEntities: 0, expectedEvents: 1);
        Assert.Null(await _queue.Dequeue());
    }
    
    [Fact]
    public async Task OutboxBatchDelayedMessageNotVisible() {
        _testDbContext.Add(new TestEntity { Id = _businessId });
        await _testDbContext.SaveAndEnqueueAsync(batch => batch
            .Add(_queue, new TestEvent(_businessId))
            .Add(_queue2, new TestEvent2(_businessId2), enqueueAt: DateTime.UtcNow.AddHours(1))
        );

        var message1 = await _queue.Dequeue();
        var message2 = await _queue2.Dequeue();
        Assert.NotNull(message1);
        Assert.Null(message2);
    }

    private async Task<(List<TestEntity> entities, List<DbQMessage> events)> GetDatabaseEntities() {
        using var scope = _host.Services.CreateScope();
        var testDbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var taskDbContext = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
        var businessEntities = await testDbContext.TestEntities.ToListAsync();
        var events = await taskDbContext.Queue.ToListAsync();

        return (businessEntities, events);
    }

    private async Task AssertDatabaseData(Guid businessId, int expectedEntities, int expectedEvents) {
        var (businessEntities, events) = await GetDatabaseEntities();

        Assert.Equal(expectedEntities, businessEntities.Count);
        Assert.Equal(expectedEvents, events.Count);

        Assert.Equal(expectedEntities, businessEntities.Count(x => x.Id == businessId));
        Assert.Equal(expectedEvents, events.Count(x => x.QueueName == QueueName && x.Payload.Contains(businessId.ToString())));
    }
    
    #endregion

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

    public class TestDbContext : DbContext
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

        var taskDbContext = _scope.ServiceProvider.GetRequiredService<TaskDbContext>();
        await taskDbContext.Database.EnsureCreatedAsync();

        var databaseCreator = (RelationalDatabaseCreator)_testDbContext.Database.GetService<IDatabaseCreator>();
        await databaseCreator.CreateTablesAsync();
    }

    public async Task DisposeAsync() {
        _scope.Dispose();
        using (var scope = _host.Services.CreateScope()) {
            await scope.ServiceProvider.GetRequiredService<TaskDbContext>().Database.EnsureDeletedAsync();
        }
        await _host.StopAsync();
        _host.Dispose();
    }
}