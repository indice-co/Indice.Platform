using System.Runtime.CompilerServices;
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
            var completed = new ChatConversationCompletedStep();
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

    class ChatConversationCompletedStep() : Executor<ConversationOutput, ConversationOutput>("ChatConversationCompleted")
    {

        public override async ValueTask<ConversationOutput> HandleAsync(ConversationOutput message, IWorkflowContext context, CancellationToken cancellationToken = default) {
            // await context.Say(Id, "Done");
            return message;
        }
    }


    public record ConversationOutput(bool Done);


    public static class HumanInTheLoopConstants
    {
        public const string ContentTypeMask = "application/vnd.indice.hitl-{0}+json";
        public static readonly string RequestContentType = string.Format(ContentTypeMask, "request");
        public static readonly string ResponseContentType = string.Format(ContentTypeMask, "response");

    }

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
        await foreach (var evt in run.WatchStreamAsync().WithCancellation(cancellationToken)) {
            switch (evt) {
                case AgentResponseUpdateEvent updateEvent:
                    var update = updateEvent.Update.AsChatResponseUpdate();
                    update.ConversationId = options!.ConversationId;
                    yield return update;
                    break;
                case RequestInfoEvent requestInfoEvent when !message.HasFunctionResultContent(requestInfoEvent.Request.RequestId):
                    var lastCheckPoint = await checkpointManager.GetLatestCheckpointAsync(sessionId, cancellationToken);
                    var requestUpdate = requestInfoEvent.AsAgentResponseUpdate(lastCheckPoint!)
                                                        .AsChatResponseUpdate();
                    requestUpdate.ConversationId = options!.ConversationId;
                    yield return requestUpdate;
                    yield break;
                case RequestInfoEvent requestInfoEvent when message.HasFunctionResultContent(requestInfoEvent.Request.RequestId):
                    var response = requestInfoEvent.Request.CreateResponse(message.GetFunctionResult()!);
                    await run.SendResponseAsync(response);
                    break;
                default:
                    break;
            }
        }
        cancellationToken.ThrowIfCancellationRequested();
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null ? ServiceProvider.GetService(serviceType) 
                           : ServiceProvider.GetKeyedService(serviceType, serviceKey);
    
    public void Dispose() { }
}