using Azure.AI.OpenAI;
using Indice.Features.Agents.Core.Models.Cases;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows.Mcp;
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
    private readonly string _model;

    /// <summary>Creates a new <see cref="OtpRetryChallengeBuilder"/>.</summary>
    public OtpRetryChallengeBuilder(
        AzureOpenAIClient openAIClient,
        IOptions<AgentsOptions> options,
        IOptions<ModelsOptions> models,
        UserClaimsAIContextProvider userClaimsProvider,
        IMcpToolsRegistry mcpToolsRegistry) : base(nameof(OtpRetryChallengeBuilder)) {
        _openAIClient = openAIClient;
        _options = options.Value;
        _models = models.Value;
        _userClaimsProvider = userClaimsProvider;
        _mcpToolsRegistry = mcpToolsRegistry;
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
            ? $"Your previous code expired. I sent a new verification code to {MaskPhone(phoneNumber)}. Please enter the new OTP."
            : $"Please try again and enter the OTP code ({Math.Max(input.MaxFailedAttempts - input.FailedAttempts + 1, 0)} attempt(s) left).";

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
        chatOptions.Tools = [..(chatOptions.Tools ?? []), ..mcpTools];

        var agent = _openAIClient
            .GetChatClient(_model)
            .AsIChatClient()
            .AsAIAgent(options: new ChatClientAgentOptions {
                ChatOptions = chatOptions,
                AIContextProviders = [_userClaimsProvider],
                Name = "DexOtpRetrySenderAgent"
            });

        var sendPrompt = $"""
            Send an OTP now by calling SendTotp with the configured fixed values.
            Use phone number: {phoneNumber}
            And securityToken: {caseId}
            Do not verify now.
            """;

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
