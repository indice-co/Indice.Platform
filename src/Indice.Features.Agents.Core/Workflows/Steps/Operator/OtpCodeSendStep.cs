using Azure.AI.OpenAI;
using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Workflows.Ports;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

/// <summary>
/// Sends an OTP using MCP tools and produces a challenge prompt.
/// The workflow pauses after this step and waits for the user OTP input on a request port.
/// </summary>
public sealed class OtpCodeSendStep : Executor<ChatMessage, OtpRequestPort.OtpRequest>
{
    private readonly AzureOpenAIClient _openAIClient;
    private readonly AgentsOptions _options;
    private readonly ModelsOptions _models;
    private readonly IPromptTemplateRenderer _prompts;
    private readonly UserClaimsAIContextProvider _userClaimsProvider;
    private readonly IMcpClientFactory _mcpClientFactory;
    private readonly AgentMessageLocalizer _messageLocalizer;
    private readonly string _model;

    /// <summary>Creates a new <see cref="OtpCodeSendStep"/>.</summary>
    public OtpCodeSendStep(
        AzureOpenAIClient openAIClient,
        IOptions<AgentsOptions> options,
        IOptions<ModelsOptions> models,
        IPromptTemplateRenderer prompts,
        UserClaimsAIContextProvider userClaimsProvider,
        [FromKeyedServices("id")] IMcpClientFactory mcpClientFactory,
        AgentMessageLocalizer messageLocalizer) : base(nameof(OtpCodeSendStep)) {
        _openAIClient = openAIClient;
        _options = options.Value;
        _models = models.Value;
        _prompts = prompts;
        _userClaimsProvider = userClaimsProvider;
        _mcpClientFactory = mcpClientFactory;
        _model = _options.AzureOpenAI.Deployments.Reasoning!;
        _messageLocalizer = messageLocalizer;
    }

    /// <inheritdoc/>
    public override async ValueTask<OtpRequestPort.OtpRequest> HandleAsync(
        ChatMessage validationData,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {

        ArgumentNullException.ThrowIfNull(validationData);
        var caseData = await context.GetOperatorStateAsync(cancellationToken);
        var maskedPhoneNumber = MaskPhone(caseData.PhoneNumber);
        var securityToken = Guid.NewGuid().ToString();
        //await SendOtpCode(caseData, securityToken, cancellationToken);
        var otpPrompt = _messageLocalizer.OtpVerificationCodeSendMessage(maskedPhoneNumber);
        await context.Say(Id, otpPrompt);
        return new OtpRequestPort.OtpRequest(ChallengeCode: securityToken, ExpirationDate: DateTime.UtcNow.AddMinutes(2));
    }

    private async Task<string> SendOtpCode(OperatorState caseData, string securityToken, CancellationToken cancellationToken) {

        // Fetch OTP tools from the Identity MCP server at runtime.
        var registry = await _mcpClientFactory.CreateAsync();
        var mcpTools = await registry.ListToolsAsync(options: null, cancellationToken);
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
                Name = "DexOtpAgent"
            });
        // Execute only the send leg now; OTP code collection is done by the workflow host via RequestPort.
        var sendPrompt = _prompts.Render(nameof(AgentsConstants.PromptDefaults.OtpCodeSenderPrompt), new {
            phoneNumber = caseData.PhoneNumber,
            email = caseData.Email,
            securityToken
        });
        var resuts = await agent.RunAsync<string>(sendPrompt, cancellationToken: cancellationToken);
        return securityToken;
    }

    private static string MaskPhone(string? phone) {
        if (string.IsNullOrWhiteSpace(phone)) {
            return "your registered phone";
        }
        var digits = System.Text.RegularExpressions.Regex.Replace(phone, @"\D", "");
        return digits.Length < 4 ? "your registered phone" : $"***{digits[^4..]}";
    }
}
