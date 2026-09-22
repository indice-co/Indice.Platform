using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Workflows.Ports;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Workflows.Demo;

/// <summary>
/// Represents a demo workflow for handling chat messages and OTP verification.
/// </summary>
public static class DemoWorkflow
{
    /// <summary>
    /// Creates a demo workflow for handling chat messages and OTP verification.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <returns>The created workflow.</returns>
    public static Workflow CreateDemoWorkflow(IServiceProvider serviceProvider) {
        var start = new ChatTurnStartStep();
        var otpRequirement = new OtpRequirementStep();
        var otpVerification = new OtpVerificationStep();
        var otpPort = OtpRequestPort.Create();
        var workflow = new WorkflowBuilder(start)
                       .AddEdge(start, otpRequirement)
                       .AddEdge(otpRequirement, otpPort)
                       .AddEdge(otpPort, otpVerification)
                       .AddEdge(otpVerification, otpPort)
                       .WithOutputFrom(otpVerification)
                       .Build();
        return workflow;
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
            return new OtpRequestPort.OtpRequest(ChallengeCode: "TestChallengeCode", ExpirationDate: DateTime.UtcNow.AddMinutes(5));
        }
    }

    [SendsMessage(typeof(OtpRequestPort.OtpRequest))]
    [YieldsOutput(typeof(ConversationOutput))]
    class OtpVerificationStep() : Executor<OtpRequestPort.OtpResponse>("OtpVerification")
    {

        public override async ValueTask HandleAsync(OtpRequestPort.OtpResponse message, IWorkflowContext context, CancellationToken cancellationToken = default) {
            if ("123456".Equals(message.Otp)) {
                await context.Say(Id, $"Otp verified {message.Otp}!");
                await context.YieldOutputAsync(new ConversationOutput(Done: true));
                return;
            }
            await context.Say(Id, $"Otp verification failed for {message.Otp}!");
            await context.SendMessageAsync(new OtpRequestPort.OtpRequest(ChallengeCode: "TestChallengeCode", ExpirationDate: DateTime.UtcNow.AddMinutes(5)));
        }
    }

    /// <summary>
    /// Represents the output of a conversation, indicating whether the conversation is done.
    /// </summary>
    /// <param name="Done">Indicates whether the conversation is done.</param>
    public record ConversationOutput(bool Done);


}
