using Azure.AI.OpenAI;
using Indice.Features.Agents.Core.Models.Cases;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps.Cases;

/// <summary>
/// Builds the next OTP challenge when validation allows a retry.
/// Sends a new OTP when the previous one expired.
/// </summary>
public sealed class OtpRetryChallengeBuilder : Executor<OtpValidationOutput, OtpChallengeOutput>
{
    private readonly AzureOpenAIClient _openAIClient;
    private readonly AgentsOptions _options;
    private readonly ModelsOptions _models;
    private readonly UserClaimsAIContextProvider _userClaimsProvider;
    private readonly IMcpToolsRegistry _mcpToolsRegistry;
    private readonly AgentMessageLocalizer _messageLocalizer;
    private readonly IPromptTemplateRenderer _prompts;
    private readonly string _model;

    /// <summary>Creates a new <see cref="OtpRetryChallengeBuilder"/>.</summary>
    public OtpRetryChallengeBuilder(
        AzureOpenAIClient openAIClient,
        IOptions<AgentsOptions> options,
        IOptions<ModelsOptions> models,
        UserClaimsAIContextProvider userClaimsProvider,
        IMcpToolsRegistry mcpToolsRegistry,
        IPromptTemplateRenderer prompts,
        AgentMessageLocalizer messageLocalizer  ) : base(nameof(OtpRetryChallengeBuilder)) {
        _openAIClient = openAIClient;
        _options = options.Value;
        _models = models.Value;
        _userClaimsProvider = userClaimsProvider;
        _mcpToolsRegistry = mcpToolsRegistry;
        _messageLocalizer = messageLocalizer;
        _prompts = prompts;
        _model = _options.AzureOpenAI.Deployments.Reasoning!;
    }

    /// <inheritdoc/>
    public override async ValueTask<OtpChallengeOutput> HandleAsync(
        OtpValidationOutput input,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {

        ArgumentNullException.ThrowIfNull(input);

        if (!input.ShouldRetry) {
            throw new InvalidOperationException("OTP retry challenge requested while retry is not allowed.");
        }

        var previousChallenge = input.OtpResponse.Challenge;
        var phoneNumber = previousChallenge.PhoneNumber;
        var caseId = previousChallenge.CaseId;

        var prompt = input.ShouldResendOtp
            ? _messageLocalizer.OtpVerificationCodeSendMessage(MaskPhone(phoneNumber))
            : _messageLocalizer.InvalidOtpRetryMessage(Math.Max(input.MaxFailedAttempts - input.FailedAttempts + 1, 0));

        if (input.ShouldResendOtp) {
            await SendOtpAsync(phoneNumber, caseId, cancellationToken);
        }

        return previousChallenge with {
            Prompt = prompt,
            FailedAttempts = input.FailedAttempts,
            MaxFailedAttempts = input.MaxFailedAttempts
        };
    }

    private async Task SendOtpAsync(string? phoneNumber, string caseId, CancellationToken cancellationToken) {
        var mcpTools = await _mcpToolsRegistry.GetToolsAsync("Identity", cancellationToken);
        if (mcpTools.Count == 0) {
            throw new InvalidOperationException("No MCP tools discovered for service 'Identity'.");
        }


        
        var chatOptions = _models.BaseReasoningModelOptions.Clone();
        chatOptions.Instructions = _prompts.Render(nameof(AgentsConstants.PromptDefaults.OtpCodeSenderInstructions));
        chatOptions.Tools = [.. (chatOptions.Tools ?? []), .. mcpTools];

        var agent = _openAIClient
            .GetChatClient(_model)
            .AsIChatClient()
            .AsAIAgent(options: new ChatClientAgentOptions() {
                ChatOptions = chatOptions,
                AIContextProviders = [_userClaimsProvider],
                Name = "DexOtpRetrySenderAgent"
            });

        // Execute only the send leg now; OTP code collection is done by the workflow host via RequestPort.
        var sendPrompt = _prompts.Render(nameof(AgentsConstants.PromptDefaults.OtpCodeSenderPrompt), new {
            phoneNumber = phoneNumber,
            //email = .Email,
            securityToken = caseId
        });
        _ = await agent.RunAsync<string>(sendPrompt, cancellationToken: cancellationToken);
    }

    private static string MaskPhone(string? phone) {
        if (string.IsNullOrWhiteSpace(phone)) {
            return "your registered phone";
        }
        var digits = System.Text.RegularExpressions.Regex.Replace(phone, @"\D", "");
        return digits.Length < 4 ? "your registered phone" : $"***{digits[^4..]}";
    }
}
