using Indice.Features.Agents.Core.Extensions;
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
        var otpPort = OtpRequestPort.CreateOtpPort();
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


    /// <summary>
    /// Represents a request port for OTP (One-Time Password) verification in the workflow.
    /// </summary>
    public static class OtpRequestPort
    {
        /// <summary>
        /// Represents a request for an OTP (One-Time Password) with an expiration date.
        /// </summary>
        /// <param name="ChallengeCode">The challenge code of the OTP.</param>
        /// <param name="ExpirationDate">The expiration date of the OTP.</param>
        public record OtpRequest(string ChallengeCode, DateTime ExpirationDate);

        /// <summary>
        /// Represents a response containing an OTP (One-Time Password).
        /// </summary>
        /// <param name="ChallengeCode">The challenge code of the OTP.</param>
        /// <param name="Otp">The OTP (One-Time Password).</param>
        public record OtpResponse(string ChallengeCode, string Otp);

        /// <summary>
        /// Creates a request port for OTP (One-Time Password) verification in the workflow.
        /// </summary>
        /// <param name="id">The identifier for the request port.</param>
        /// <returns>A request port for OTP verification.</returns>
        public static RequestPort<OtpRequest, OtpResponse> CreateOtpPort(string id = nameof(OtpRequest)) => RequestPort.Create<OtpRequest, OtpResponse>(id);
    }

    /// <summary>
    /// Represents a request port for Ownership verification in the workflow.
    /// </summary>
    public static class OwnershipVerificationRequestPort
    {
        /// <summary>
        /// Represents a request for an verfication request
        /// </summary>
        /// <param name="message">The message for the user.</param>
        public record OwnershipVerificationRequest(string message);

        /// <summary>
        /// Represents a response for an verfication request containing users input
        /// </summary>
        /// <param name="userInput">Users input.</param>
        public record OwnershipVerificationResponse(string userInput);

        /// <summary>
        /// Creates a request port for OTP (One-Time Password) verification in the workflow.
        /// </summary>
        /// <param name="id">The identifier for the request port.</param>
        /// <returns>A request port for OTP verification.</returns>
        public static RequestPort<OwnershipVerificationRequest, OwnershipVerificationResponse> CreateOwnershipPort(string id = nameof(OwnershipVerificationRequestPort)) => RequestPort.Create<OwnershipVerificationRequest, OwnershipVerificationResponse>(id);
    }
}
