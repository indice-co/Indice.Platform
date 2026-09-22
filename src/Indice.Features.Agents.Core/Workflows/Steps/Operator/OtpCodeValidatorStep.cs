using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using static Indice.Features.Agents.Core.Workflows.Demo.DemoWorkflow;

namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

/// <summary>
/// Verifies a user-provided OTP code using MCP tools and produces the terminal response.
/// </summary>
[SendsMessage(typeof(OtpRequestPort.OtpRequest))]
[SendsMessage(typeof(ChatMessage))]
[YieldsOutput(typeof(ValidationFailureOutput))]
public sealed class OtpCodeValidatorStep : Executor<OtpRequestPort.OtpResponse>
{
    private const string McpServiceKey = "Identity";

    private readonly int _maxValidationAttempts;
    private readonly AzureOpenAIClient _openAIClient;
    private readonly AgentsOptions _options;
    private readonly ModelsOptions _models;
    private readonly UserClaimsAIContextProvider _userClaimsProvider;
    private readonly IMcpClientFactory _mcpClientFactory;
    private readonly AgentMessageLocalizer _messageLocalizer;
    private readonly IPromptTemplateRenderer _prompts;
    private readonly string _model;

    /// <summary>Creates a new <see cref="OtpCodeValidatorStep"/>.</summary>
    public OtpCodeValidatorStep(
        AzureOpenAIClient openAIClient,
        IOptions<AgentsOptions> options,
        IOptions<ModelsOptions> models,
        UserClaimsAIContextProvider userClaimsProvider,
        [FromKeyedServices("id")] IMcpClientFactory mcpClientFactory,
        AgentMessageLocalizer messageLocalizer,
        IPromptTemplateRenderer prompts) : base(nameof(OtpCodeValidatorStep)) {
        _openAIClient = openAIClient;
        _options = options.Value;
        _models = models.Value;
        _userClaimsProvider = userClaimsProvider;
        _mcpClientFactory = mcpClientFactory;
        _messageLocalizer = messageLocalizer;
        _prompts = prompts;
        _model = _options.AzureOpenAI.Deployments.Reasoning!; 
        _maxValidationAttempts = options.Value.CasesWorkflow.MaxOtpValidationAttempts;
    }

    /// <inheritdoc/>
    public override async ValueTask HandleAsync(
        OtpRequestPort.OtpResponse response,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(response);
        
        if (string.IsNullOrWhiteSpace(response.Otp?.Trim())) {
            await context.Say(Id, _messageLocalizer.OtpInputValidationEmpty);
            await context.SendMessageAsync(new OtpRequestPort.OtpRequest(response.ChallengeCode, DateTime.UtcNow));
            return;
        }

        var caseData = await context.GetOperatorStateAsync(cancellationToken);
        //var payload = await ValidateOtp(response, caseData, cancellationToken);

        //OtpVerificationResultPayload verification;
        //try {
        //    verification = OtpVerificationResultPayload.Deserialize(payload);

        //} catch (JsonException) {
        //    verification = new OtpVerificationResultPayload(false, $"MCP results is not valid:{payload}", false, false, false, 0);
        //}
        OtpVerificationResultPayload verification = new OtpVerificationResultPayload(
            Success: false,
            Error: null,
            IsRateLimited: false,
            IsInvalidCode: response.Otp != "123456",
            IsInvalidFormat: false,
            TotpLifetime: 30);

        if (verification.Success) {
            await context.Say(Id, _messageLocalizer.OtvpVerificationSuccessMessage);
            await context.SendMessageAsync(new ChatMessage(ChatRole.Assistant, [
                new TextContent(_messageLocalizer.OtvpVerificationSuccessMessage)]));
            return;
        }
        var attempt = (await context.GetApprovalStateAsync(cancellationToken)) + 1;
        await context.SetApprovalStateAsync(attempt, cancellationToken);
        if (attempt >= _maxValidationAttempts) {
            await context.Say(Id, _messageLocalizer.InvalidOtpMaxAttemptsReachedMessage);
            await context.YieldOutputAsync(new ValidationFailureOutput(
                        ErrorMessage: _messageLocalizer.InvalidOtpMaxAttemptsReachedMessage,
                        FailureStep: "OtpValidation"));
            return;
        }

        await context.Say(Id, _messageLocalizer.InvalidOtpRetryMessage(Math.Max(_maxValidationAttempts - attempt, 0)));
        await context.SendMessageAsync(new OtpRequestPort.OtpRequest(response.ChallengeCode, DateTime.UtcNow));

        //var shouldRetry = !verification.IsRateLimited && attempt<= _maxValidationAttempts;
        //var finalMessage = shouldRetry
        //    ? _messageLocalizer.InvalidOtpRetryMessage(Math.Max(_maxValidationAttempts - attempt, 0))

        //if(verification.IsRateLimited) {
        //    finalMessage = _messageLocalizer.InvalidOtpRateLimitMessage;
        //}
        //if (!shouldRetry) {
        //    await context.AddEventAsync(new AnswerDeltaEvent(finalMessage), cancellationToken);
        //}

    }

    private async Task<string> ValidateOtp(OtpRequestPort.OtpResponse response, OperatorState caseData, CancellationToken cancellationToken) {

        // Fetch OTP tools from the Identity MCP server at runtime.
        var registry = await _mcpClientFactory.CreateAsync(cancellationToken);
        var mcpTools = await registry.ListToolsAsync(options: null, cancellationToken);
        if (mcpTools.Count == 0) {
            throw new InvalidOperationException($"No MCP tools discovered for service '{McpServiceKey}'.");
        }

        var chatOptions = _models.BaseReasoningModelOptions.Clone();
        chatOptions.Instructions = _prompts.Render(nameof(AgentsConstants.PromptDefaults.OtpCodeValidatorInstructions));
        chatOptions.Tools = [.. (chatOptions.Tools ?? []), .. mcpTools];

        var agent = _openAIClient
            .GetChatClient(_model)
            .AsIChatClient()
            .AsAIAgent(options: new ChatClientAgentOptions {
                ChatOptions = chatOptions,
                AIContextProviders = [_userClaimsProvider],
                Name = "DexOtpCodeValidatorAgent"
            });

        var prompt = _prompts.Render(nameof(AgentsConstants.PromptDefaults.OtpCodeValidatorPrompt), new {
            otp = response.Otp.Trim(),
            caseId = response.ChallengeCode,
            phoneNumber = caseData.PhoneNumber
        });
        var result = await agent.RunAsync<string>(prompt, cancellationToken: cancellationToken);
        return result.Text;
    }
}



/// <summary>
/// Raw OTP verification payload returned by the MCP verification tool.
/// </summary>
public sealed record OtpVerificationResultPayload(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("error")] string? Error,
    [property: JsonPropertyName("isRateLimited")] bool IsRateLimited,
    [property: JsonPropertyName("isInvalidCode")] bool IsInvalidCode,
    [property: JsonPropertyName("isInvalidFormat")] bool IsInvalidFormat,
    [property: JsonPropertyName("totpLifetime")] int TotpLifetime)
{
    private static readonly JsonSerializerOptions SerializerOptions = new() {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Deserializes a JSON OTP verification payload.
    /// </summary>
    /// <param name="json">The JSON payload to parse.</param>
    /// <returns>A strongly typed OTP verification payload.</returns>
    public static OtpVerificationResultPayload Deserialize(string json) =>
        JsonSerializer.Deserialize<OtpVerificationResultPayload>(json, SerializerOptions)
        ?? throw new InvalidOperationException("Empty JSON payload.");
}
