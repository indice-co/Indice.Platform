using System.Runtime.CompilerServices;
using System.Text.Json;
using Indice.Features.Agents.Core.Data;
using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Workflows;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Indice.Features.Agents.Core.Tests;

public class WorkflowTests
{

    public WorkflowTests() {

    }

    [Fact]
    public async Task HumanInTheLoopWorkflowTest() {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
          .AddInMemoryCollection(new Dictionary<string, string?> {
              ["test"] = "test"
          })
          .Build();
        services.AddSingleton<IConfiguration>(configuration); 
        services.AddDbContext<AgentsDbContext>(builder => builder.UseInMemoryDatabase(databaseName: "AgentsDb"), ServiceLifetime.Singleton);
        services.AddSingleton<PersistedCheckpointStore>();
        services.TryAddTransient(sp => CheckpointManager.CreateJson(sp.GetRequiredService<PersistedCheckpointStore>()));
        services.AddKeyedTransient("default", (sp, key) => {
            var start = new ChatTurnStartStep();
            var otpRequirement = new OtpRequirementStep();
            var otpVerification = new OtpVerificationStep();
            var otpPort = OtpRequestPort.CreateOtpPort();
            var workflow = new WorkflowBuilder(start)
                           .AddEdge(start, otpRequirement)
                           .AddEdge(otpRequirement, otpPort)
                           .AddEdge(otpPort, otpVerification)
                           .AddEdge(otpVerification, otpPort)
                           .WithOutputFrom(otpVerification)
                           .Build();
            return workflow;
        });
        services.AddTransient<IChatClient, WorkflowChatClient>();
        var serviceProvider = services.BuildServiceProvider();
        
        var chatClient = serviceProvider.GetRequiredService<IChatClient>();
        var chatOptions = new ChatOptions { Instructions = "default", ConversationId = Guid.NewGuid().ToString() };
        var messageA = new ChatMessage(ChatRole.User, "Hello! I need to validate my identity.") { AuthorName = "John Doe" };
        var responseA = await chatClient.GetResponseAsync([ messageA ], chatOptions, TestContext.Current.CancellationToken);

        var functionCallContentA = responseA.Messages.First().Contents.OfType<FunctionCallContent>().First();
        
        var messageB = new ChatMessage(ChatRole.User, [
            new TextContent("Here is my otp."), 
            new FunctionResultContent(functionCallContentA.CallId, new OtpRequestPort.OtpResponse ("1234567"))
            ]) { AuthorName = "John Doe" };
        var responseB = await chatClient.GetResponseAsync([messageB], chatOptions, TestContext.Current.CancellationToken);

        Assert.Equal("Otp verification failed for 1234567!", responseB.ToString());

        var functionCallContentB = responseB.Messages.First().Contents.OfType<FunctionCallContent>().First();

        Assert.NotEqual(functionCallContentA.CallId, functionCallContentB.CallId);

        var messageC = new ChatMessage(ChatRole.User, [
            new TextContent("Here is my otp."), 
            new FunctionResultContent(functionCallContentB.CallId, new OtpRequestPort.OtpResponse("123456"))
            ]) { AuthorName = "John Doe" };
        var responseC = await chatClient.GetResponseAsync([messageC], chatOptions, TestContext.Current.CancellationToken);
        
        Assert.Equal("Otp verified 123456!", responseC.ToString());

        await serviceProvider.DisposeAsync();
    }

    [Fact]
    public async Task HumanInTheLoopWorkflow_WithSerializedResult_Test() {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
          .AddInMemoryCollection(new Dictionary<string, string?> {
              ["test"] = "test"
          })
          .Build();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddDbContext<AgentsDbContext>(builder => builder.UseInMemoryDatabase(databaseName: "AgentsDb"), ServiceLifetime.Singleton);
        services.AddSingleton<PersistedCheckpointStore>();
        services.TryAddTransient(sp => CheckpointManager.CreateJson(sp.GetRequiredService<PersistedCheckpointStore>()));
        services.AddKeyedTransient("default", (sp, key) => {
            var start = new ChatTurnStartStep();
            var otpRequirement = new OtpRequirementStep();
            var otpVerification = new OtpVerificationStep();
            var otpPort = OtpRequestPort.CreateOtpPort();
            var workflow = new WorkflowBuilder(start)
                           .AddEdge(start, otpRequirement)
                           .AddEdge(otpRequirement, otpPort)
                           .AddEdge(otpPort, otpVerification)
                           .AddEdge(otpVerification, otpPort)
                           .WithOutputFrom(otpVerification)
                           .Build();
            return workflow;
        });
        services.AddTransient<IChatClient, WorkflowChatClient>();
        var serviceProvider = services.BuildServiceProvider();

        var chatClient = serviceProvider.GetRequiredService<IChatClient>();
        var chatOptions = new ChatOptions { Instructions = "default", ConversationId = Guid.NewGuid().ToString() };
        var messageA = new ChatMessage(ChatRole.User, "Hello! I need to validate my identity.") { AuthorName = "John Doe" };
        var responseA = await chatClient.GetResponseAsync([messageA], chatOptions, TestContext.Current.CancellationToken);

        var functionCallContentA = responseA.Messages.First().Contents.OfType<FunctionCallContent>().First();


        var jsonResult = JsonElement.Parse(JsonSerializer.Serialize(new OtpRequestPort.OtpResponse("123456"), new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var messageB = new ChatMessage(ChatRole.User, [
            new TextContent("Here is my otp."),
            new FunctionResultContent(functionCallContentA.CallId, jsonResult)
            ]) { AuthorName = "John Doe" };
        var responseB = await chatClient.GetResponseAsync([messageB], chatOptions, TestContext.Current.CancellationToken);

        Assert.Equal("Otp verified 123456!", responseB.ToString());
        
        await serviceProvider.DisposeAsync();
    }


    [Theory(Timeout = 30_000)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public async Task OwnershipWorkflow_ValidatesInput_RetriesOrCompletes(int failedAttempts, bool succeeds) {
        const int maxAttempts = 3;
        var nextStepExecutions = 0;
        await using var serviceProvider = CreateOwnershipServiceProvider(maxAttempts, () => nextStepExecutions++);
        var outputs = new List<WorkflowOutputEvent>();
        var streamCompleted = false;
        using var chatClient = new WorkflowChatClient(serviceProvider) {
            OnOutput = outputs.Add,
            OnStreamCompleted = () => streamCompleted = true
        };
        var chatOptions = new ChatOptions { Instructions = "ownership", ConversationId = Guid.NewGuid().ToString() };
        var cancellationToken = TestContext.Current.CancellationToken;
        var response = await chatClient.GetResponseAsync([new ChatMessage(ChatRole.User, "Verify ownership.")], chatOptions, cancellationToken);
        var callIds = new HashSet<string>();
        var inputs = Enumerable.Repeat("invalid", failedAttempts).Concat(succeeds ? ["valid"] : Array.Empty<string>()).ToArray();

        for (var index = 0; index < inputs.Length; index++) {
            var request = Assert.Single(response.Messages.SelectMany(message => message.Contents).OfType<FunctionCallContent>());
            Assert.Equal("OwnershipPort", request.Name);
            Assert.True(callIds.Add(request.CallId), "Every retry must use a fresh request call ID.");
            Assert.Equal(0, nextStepExecutions);
            Assert.Empty(outputs);
            Assert.False(streamCompleted);

            var reply = new ChatMessage(ChatRole.User, [new FunctionResultContent(request.CallId, new OwnershipResponse(inputs[index]))]);
            response = await chatClient.GetResponseAsync([reply], chatOptions, cancellationToken);

            if (index < inputs.Length - 1) {
                Assert.Equal($"Ownership verification failed ({index + 1}/{maxAttempts}).", response.ToString());
            }
        }

        Assert.Empty(response.Messages.SelectMany(message => message.Contents).OfType<FunctionCallContent>());
        Assert.Equal(succeeds ? 1 : 0, nextStepExecutions);
        Assert.Equal(succeeds ? "Ownership verified. Next step executed." : $"Ownership verification failed after {maxAttempts} attempts.", response.ToString());
        var output = Assert.IsType<OwnershipOutput>(Assert.Single(outputs).Data);
        Assert.Equal(new OwnershipOutput(Done: true, Verified: succeeds, failedAttempts), output);
        Assert.True(streamCompleted);
    }

    private static ServiceProvider CreateOwnershipServiceProvider(int maxAttempts, Action onNextStep) {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddDbContext<AgentsDbContext>(builder => builder.UseInMemoryDatabase(databaseName), ServiceLifetime.Singleton);
        services.AddSingleton<PersistedCheckpointStore>();
        services.AddTransient(sp => CheckpointManager.CreateJson(sp.GetRequiredService<PersistedCheckpointStore>()));
        services.AddKeyedTransient("ownership", (sp, key) => {
            var requirement = new OwnershipRequirementStep();
            var ownershipPort = RequestPort.Create<OwnershipRequest, OwnershipResponse>("OwnershipPort");
            var validator = new OwnershipVerificationStep(maxAttempts);
            var nextStep = new OwnershipNextStep(onNextStep);
            return new WorkflowBuilder(requirement)
                .AddEdge(requirement, ownershipPort)
                .AddEdge(ownershipPort, validator)
                .AddSwitch(validator, sw => sw
                    .AddCase<OwnershipVerified>(message => message is not null, nextStep)
                    .WithDefault(ownershipPort))
                .WithOutputFrom(validator, nextStep)
                .Build();
        });
        return services.BuildServiceProvider();
    }

    class OwnershipRequirementStep() : Executor<ChatMessage, OwnershipRequest>("OwnershipRequirement")
    {
        public override ValueTask<OwnershipRequest> HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default) {
            return ValueTask.FromResult(new OwnershipRequest("Enter the ownership value."));
        }
    }

    [SendsMessage(typeof(OwnershipRequest))]
    [SendsMessage(typeof(OwnershipVerified))]
    [YieldsOutput(typeof(OwnershipOutput))]
    class OwnershipVerificationStep(int maxAttempts) : Executor<OwnershipResponse>("OwnershipVerification")
    {
        public override async ValueTask HandleAsync(OwnershipResponse message, IWorkflowContext context, CancellationToken cancellationToken = default) {
            var failedAttempts = await context.ReadStateAsync<int?>("FailedAttempts", cancellationToken: cancellationToken) ?? 0;
            if ("valid".Equals(message.Value, StringComparison.Ordinal)) {
                await context.SendMessageAsync(new OwnershipVerified(failedAttempts), cancellationToken: cancellationToken);
                return;
            }
            failedAttempts++;
            await context.QueueStateUpdateAsync("FailedAttempts", failedAttempts, cancellationToken: cancellationToken);
            if (failedAttempts >= maxAttempts) {
                await context.Say(Id, $"Ownership verification failed after {failedAttempts} attempts.");
                await context.YieldOutputAsync(new OwnershipOutput(Done: true, Verified: false, failedAttempts), cancellationToken);
                return;
            }
            await context.Say(Id, $"Ownership verification failed ({failedAttempts}/{maxAttempts}).");
            await context.SendMessageAsync(new OwnershipRequest("Invalid ownership value. Try again."), cancellationToken: cancellationToken);
        }
    }

    [YieldsOutput(typeof(OwnershipOutput))]
    class OwnershipNextStep(Action onExecute) : Executor<OwnershipVerified>("OwnershipNext")
    {
        public override async ValueTask HandleAsync(OwnershipVerified message, IWorkflowContext context, CancellationToken cancellationToken = default) {
            onExecute();
            await context.Say(Id, "Ownership verified. Next step executed.");
            await context.YieldOutputAsync(new OwnershipOutput(Done: true, Verified: true, message.FailedAttempts), cancellationToken);
        }
    }

    public record OwnershipRequest(string Prompt);
    public record OwnershipResponse(string Value);
    public record OwnershipVerified(int FailedAttempts);
    public record OwnershipOutput(bool Done, bool Verified, int FailedAttempts);

    class ChatTurnStartStep() : Executor<ChatMessage, ChatMessage>("ChatTurnStart")
    {
        
        public override async ValueTask<ChatMessage> HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default) {
            await context.Say(Id, $"Hello {message.AuthorName}!");
            return message;
        }
    }

    class OtpRequirementStep() : Executor<ChatMessage, OtpRequestPort.OtpRequest>("OtpRequirement")
    {

        public override async ValueTask<OtpRequestPort.OtpRequest> HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default) {
            await context.Say(Id, $"Hello {message.AuthorName}! I need an otp to validate ");
            return new OtpRequestPort.OtpRequest(ExpirationDate: DateTime.UtcNow.AddMinutes(5));
        }
    }

    [SendsMessage(typeof(OtpRequestPort.OtpRequest))]
    [YieldsOutput(typeof(ConversationOutput))]
    class OtpVerificationStep(): Executor<OtpRequestPort.OtpResponse>("OtpVerification")
    {

        public override async ValueTask HandleAsync(OtpRequestPort.OtpResponse message, IWorkflowContext context, CancellationToken cancellationToken = default) {
            if ("123456".Equals(message.Otp)) { 
                await context.Say(Id, $"Otp verified {message.Otp}!");
                await context.YieldOutputAsync(new ConversationOutput(Done: true));
                return;
            }
            await context.Say(Id, $"Otp verification failed for {message.Otp}!");
            await context.SendMessageAsync(new OtpRequestPort.OtpRequest(ExpirationDate: DateTime.UtcNow.AddMinutes(5)));
        }
    }

    public record ConversationOutput(bool Done);

    public static class OtpRequestPort 
    { 
        public record OtpRequest(DateTime ExpirationDate);
        public record OtpResponse(string Otp);

        public static RequestPort<OtpRequest, OtpResponse> CreateOtpPort(string id = nameof(OtpRequest)) => RequestPort.Create<OtpRequest, OtpResponse>(id);
    }
}

public class WorkflowChatClient(IServiceProvider serviceProvider) : IChatClient
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;
    public Action<WorkflowOutputEvent>? OnOutput { get; init; }
    public Action? OnStreamCompleted { get; init; }

    /// <inheritdoc/>
    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) {
        var stream = GetStreamingResponseAsync(messages, options, cancellationToken);
        return await stream.ToChatResponseAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
        var message = messages.First();
        options ??= new ChatOptions();
        options.ConversationId ??= Guid.NewGuid().ToString();
        options.Instructions ??= "default";
        var sessionId = new AgentSessionId(Guid.Parse(options!.ConversationId), options.Instructions);
        var workflow = ServiceProvider.GetRequiredKeyedService<Workflow>(options.Instructions);
        var checkpointManager = ServiceProvider.GetRequiredService<CheckpointManager>();
        
        StreamingRun run;
        if (message.HasFunctionResultContent()) {
            var callId = message.GetFunctionResultContentCallId();
            var checkpointInfo = new CheckpointInfo(sessionId, callId.CheckpointId!);
            run = await InProcessExecution.ResumeStreamingAsync(workflow, checkpointInfo, checkpointManager, cancellationToken: cancellationToken);
        } else {
            run = await InProcessExecution.RunStreamingAsync(workflow, message, checkpointManager, sessionId: sessionId, cancellationToken: cancellationToken);
        }
        await using var _ = run;
        RequestInfoEvent? pendingRequest = null;
        await foreach (var evt in run.WatchStreamAsync().WithCancellation(cancellationToken)) {
            switch (evt) {
                case AgentResponseUpdateEvent updateEvent:
                    var update = updateEvent.Update.AsChatResponseUpdate();
                    update.ConversationId = options!.ConversationId;
                    yield return update;
                    break;
                case RequestInfoEvent requestInfoEvent when !message.HasFunctionResultContent(requestInfoEvent.Request.RequestId):
                    // Defer emission until the superstep checkpoint is committed (raised via SuperStepCompletedEvent).
                    pendingRequest = requestInfoEvent;
                    break;
                case SuperStepCompletedEvent superStepCompleted when pendingRequest is not null && superStepCompleted.CompletionInfo?.Checkpoint is not null:
                    var requestUpdate = pendingRequest.AsAgentResponseUpdate(superStepCompleted.CompletionInfo.Checkpoint)
                                                      .AsChatResponseUpdate();
                    requestUpdate.ConversationId = options!.ConversationId;
                    yield return requestUpdate;
                    yield break;
                case RequestInfoEvent requestInfoEvent when message.HasFunctionResultContent(requestInfoEvent.Request.RequestId):
                    var response = requestInfoEvent.Request.CreateResponse(message.GetFunctionResult(requestInfoEvent.Request.PortInfo)!);
                    await run.SendResponseAsync(response);
                    break;
                case WorkflowOutputEvent outputEvent:
                    OnOutput?.Invoke(outputEvent);
                    break;
                default:
                    break;
            }
        }
        cancellationToken.ThrowIfCancellationRequested();
        OnStreamCompleted?.Invoke();
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null ? ServiceProvider.GetService(serviceType) 
                           : ServiceProvider.GetKeyedService(serviceType, serviceKey);
    
    public void Dispose() { }
}