using Indice.Hosting.Data;
using Indice.Hosting.Data.Models;
using Indice.Hosting.Models;
using Indice.Hosting.Services;
using Indice.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Indice.Hosting.Tests;

/// <summary>
/// Regression tests for functionality before the Outbox was introduced.
/// </summary>
public class MessageQueueIntegrationTests : IAsyncLifetime
{
    private readonly string _connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database=WorkerDb.Default.Test_{Environment.Version.Major}_{Guid.NewGuid():N};Trusted_Connection=True;MultipleActiveResultSets=true";
    private readonly string _businessConnectionString = $"Server=(localdb)\\MSSQLLocalDB;Database=WorkerDb.Default.Business_{Environment.Version.Major}_{Guid.NewGuid():N};Trusted_Connection=True;MultipleActiveResultSets=true";

    private readonly Guid _businessId = Guid.NewGuid();
    private const string QueueName = "test-event-queue";

    private readonly IHost _host;
    private IServiceScope _scope = null!;
    private TaskDbContext _taskDbContext = null!;
    private IntegratorDbContext _integratorDbContext = null!;
    private IMessageQueue<TestEvent> _queue = null!;

    public MessageQueueIntegrationTests() {
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(configuration => {
                configuration.AddInMemoryCollection(new Dictionary<string, string?> {
                    ["ConnectionStrings:WorkerDb"] = _connectionString,
                    ["General:WorkerHostDisabled"] = "true"
                });
            })
            .ConfigureServices(services => {
                services.AddDbContext<IntegratorDbContext>(options => options.UseSqlServer(_businessConnectionString));
                services.AddWorkerHost(options => {
                        options.WaitJobsToCompleteOnShutdown = true;
                        // Leave Indice.Hosting register its own TaskDbContext --> No Outbox.
                        options.UseStoreRelational(builder => builder.UseSqlServer(_connectionString));
                    })
                    .AddJob<TestEventHandler>()
                    .WithQueueTrigger<TestEvent>(options => {
                        options.QueueName = QueueName;
                        options.PollingInterval = 500;
                        options.InstanceCount = 1;
                    })
                    .AddJob<TestEventHandler>()
                    .WithScheduleTrigger<TestState>("0 0 2 * * ?", options => options.Name = "default-store-test-schedule");
                // Check registration: like in Indice.Features.Messages.Worker
                services.AddTransient<IEventDispatcher>(serviceProvider => new EventDispatcherHosting(new MessageQueueFactory(serviceProvider)));
            })
            .Build();
    }
    
    [Fact]
    public async Task EnqueueDequeueMessageIsPublishedIntegrationDbContextThrows() {
        var integratorDbContext = _scope.ServiceProvider.GetRequiredService<IntegratorDbContext>();

        integratorDbContext.TestEntities.Add(new TestEntity { Id = _businessId });
        await Assert.ThrowsAsync<NotImplementedException>(() => integratorDbContext.SaveChangesAsyncThrows());
        await _queue.Enqueue(new TestEvent(_businessId));

        var message = await _queue.Dequeue();
        Assert.NotNull(message);
        Assert.Equal(_businessId, message.Value.BusinessId);
        Assert.Empty(await integratorDbContext.TestEntities.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task EnqueueDequeueOk() {
        await _queue.Enqueue(new TestEvent(_businessId));

        Assert.Equal(1, await _queue.Count());
        var message = await _queue.Dequeue();
        Assert.NotNull(message);
        Assert.Equal(_businessId, message.Value.BusinessId);
        Assert.Equal(0, await _queue.Count());
    }

    [Fact]
    public async Task EnqueueRangeOk() {
        await _queue.EnqueueRange([new TestEvent(Guid.NewGuid()), new TestEvent(Guid.NewGuid())]);

        Assert.Equal(2, await _queue.Count());
    }

    [Fact]
    public async Task EnqueueDelayedIsNotVisible() {
        var queue = _scope.ServiceProvider.GetRequiredService<IMessageQueue<TestEvent>>();

        await queue.Enqueue(new TestEvent(Guid.NewGuid()), TimeSpan.FromHours(1));

        Assert.Null(await queue.Dequeue());
    }

    [Fact]
    public async Task EventDispatcherOk() {
        var eventDispatcher = _scope.ServiceProvider.GetRequiredService<IEventDispatcher>();

        await eventDispatcher.RaiseEventAsync(new TestEvent(_businessId));

        var taskDbContext = _scope.ServiceProvider.GetRequiredService<TaskDbContext>();
        var messages = await taskDbContext.Queue.AsNoTracking().ToListAsync();
        var message = Assert.Single(messages);
        Assert.Equal(QueueName, message.QueueName);
        Assert.Contains(_businessId.ToString(), message.Payload);
    }

    [Fact]
    public async Task ScheduledTaskStoreOk() {
        var store = _scope.ServiceProvider.GetRequiredService<IScheduledTaskStore<TestState>>();
        var task = new ScheduledTask<TestState> {
            Id = Guid.NewGuid().ToString(),
            Type = typeof(TestEventHandler).FullName!,
            State = new TestState { Watermark = 42 }
        };

        await store.Save(task);

        var savedTask = await store.GetById(task.Id);
        Assert.NotNull(savedTask);
        Assert.Equal(42, savedTask.State.Watermark);
    }
    
    [Fact]
    public async Task EnqueueWritesToTheWorkerDatabase() {
        await _queue.Enqueue(new TestEvent(_businessId));
        
        var workerMessages = await _scope.ServiceProvider.GetRequiredService<TaskDbContext>().Queue.AsNoTracking().ToListAsync();
        var message = Assert.Single(workerMessages);
        Assert.Equal(QueueName, message.QueueName);
        Assert.Contains(_businessId.ToString(), message.Payload);
    }

    [Fact]
    public async Task EventDispatcherWritesToTheWorkerDatabase() {
        var eventDispatcher = _scope.ServiceProvider.GetRequiredService<IEventDispatcher>();

        await eventDispatcher.RaiseEventAsync(new TestEvent(_businessId));

        var workerMessages = await _scope.ServiceProvider.GetRequiredService<TaskDbContext>().Queue.AsNoTracking().ToListAsync();
        Assert.Single(workerMessages);
    }
    
    public class IntegratorDbContext(DbContextOptions<IntegratorDbContext> options) : DbContext(options)
    {
        public DbSet<TestEntity> TestEntities => Set<TestEntity>();
        
        public Task<int> SaveChangesAsyncThrows(CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }
    }
    
    public class TestEntity
    {
        public Guid Id { get; set; }
    }
    
    private async Task AssertDatabaseData(Guid businessId, int expectedEntities, int expectedEvents) {
        var (businessEntities, events) = await GetDatabaseEntities();

        Assert.Equal(expectedEntities, businessEntities.Count);
        Assert.Equal(expectedEvents, events.Count);

        Assert.Equal(expectedEntities, businessEntities.Count(x => x.Id == businessId));
        Assert.Equal(expectedEvents, events.Count(x => x.QueueName == QueueName && x.Payload.Contains(businessId.ToString())));
    }
    
    private async Task<(List<TestEntity> entities, List<DbQMessage> events)> GetDatabaseEntities() {
        using var scope = _host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IntegratorDbContext>();
        var businessEntities = await dbContext.TestEntities.AsNoTracking().ToListAsync();
        var events = await dbContext.Set<DbQMessage>().AsNoTracking().ToListAsync();

        return (businessEntities, events);
    }
    
    public record TestEvent(Guid BusinessId);

    public class TestState
    {
        public int Watermark { get; set; }
    }

    public class TestEventHandler
    {
        public Task Process(TestEvent @event) => Task.CompletedTask;
    }

    public async Task InitializeAsync() {
        await _host.StartAsync();
        _scope = _host.Services.CreateScope();
        _taskDbContext = _scope.ServiceProvider.GetRequiredService<TaskDbContext>();
        _integratorDbContext = _scope.ServiceProvider.GetRequiredService<IntegratorDbContext>();
        _queue = _scope.ServiceProvider.GetRequiredService<IMessageQueue<TestEvent>>();

        await _taskDbContext.Database.EnsureCreatedAsync();
        await _integratorDbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() {
        _scope.Dispose();
        using (var scope = _host.Services.CreateScope()) {
            await scope.ServiceProvider.GetRequiredService<TaskDbContext>().Database.EnsureDeletedAsync();
            await scope.ServiceProvider.GetRequiredService<IntegratorDbContext>().Database.EnsureDeletedAsync();
        }
        await _host.StopAsync();
        _host.Dispose();
    }
}
