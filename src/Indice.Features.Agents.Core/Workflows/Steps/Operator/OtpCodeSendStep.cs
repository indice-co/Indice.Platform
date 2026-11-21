using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Providers;
using Indice.Features.Agents.Core.Workflows.Ports;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

/// <summary>
/// Sends an OTP using MCP tools and produces a challenge prompt.
/// The workflow pauses after this step and waits for the user OTP input on a request port.
/// </summary>
public class OtpCodeSendStep : Executor<OperationState, OtpRequestPort.OtpRequest>
{
    private readonly IChatClient _chatClient;
    private readonly ModelsOptions _models;
    private readonly IPromptTemplateRenderer _prompts;
    private readonly UserClaimsAIContextProvider _userClaimsProvider;
    private readonly IMcpClientFactory _mcpClientFactory;
    private readonly AgentMessageLocalizer _messageLocalizer;
    private readonly ILogger<OtpCodeSendStep> _logger;

    /// <summary>Creates a new <see cref="OtpCodeSendStep"/>.</summary>
    public OtpCodeSendStep(
        [FromKeyedServices(nameof(AgentsModelRole.Reasoning))] IChatClient chatClient,
        IOptions<ModelsOptions> models,
        IPromptTemplateRenderer prompts,
        UserClaimsAIContextProvider userClaimsProvider,
        [FromKeyedServices("id")] IMcpClientFactory mcpClientFactory,
        AgentMessageLocalizer messageLocalizer,
        ILogger<OtpCodeSendStep> logger) : base(nameof(OtpCodeSendStep)) {
        _logger = logger;
        _chatClient = chatClient;
        _models = models.Value;
        _prompts = prompts;
        _userClaimsProvider = userClaimsProvider;
        _mcpClientFactory = mcpClientFactory;
        _messageLocalizer = messageLocalizer;
    }

    /// <inheritdoc/>
    public override async ValueTask<OtpRequestPort.OtpRequest> HandleAsync(
        OperationState completed,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(completed);
        var caseData = await context.GetOperatorStateAsync(cancellationToken);
        var useSms = caseData.UsesSmsForOtp;
        var maskedRecipient = useSms ? MaskPhone(caseData.PhoneNumber) : MaskEmail(caseData.Email);

        var securityToken = Guid.NewGuid().ToString();
        if (string.IsNullOrWhiteSpace(caseData.PhoneNumber)) {
            _logger.LogWarning("No phone number for case {ReferenceId}; OTP delivery will rely on email fallback.", caseData.ReferenceId);
        }
        try {
            await SendOtpCode(caseData, useSms, securityToken, cancellationToken);
        } catch (Exception ex) when (ex is not OperationCanceledException) {
            _logger.LogError(ex, "Failed to send OTP code for case {ReferenceId}.", caseData.ReferenceId);
            throw;
        }
        var otpPrompt = _messageLocalizer.OtpVerificationCodeSendMessage(maskedRecipient);
        await context.Say(Id, otpPrompt);
        return new OtpRequestPort.OtpRequest(ChallengeCode: securityToken, ExpirationDate: DateTime.UtcNow.AddMinutes(2));
    }

    private async Task SendOtpCode(CustomerData caseData, bool sms, string securityToken, CancellationToken cancellationToken) {

        // Fetch OTP tools from the Identity MCP server at runtime.
        var registry = await _mcpClientFactory.CreateAsync();
        var mcpTools = await registry.ListToolsAsync(options: null, cancellationToken);
        if (mcpTools.Count == 0) {
            _logger.LogError("No MCP tools discovered for service 'Identity'.");
            throw new InvalidOperationException("No MCP tools discovered for service 'Identity'.");
        }

        var chatOptions = _models.BaseReasoningModelOptions.Clone();
        chatOptions.Instructions = _prompts.Render(nameof(AgentsConstants.PromptDefaults.OtpCodeSenderInstructions));
        chatOptions.Tools = [.. (chatOptions.Tools ?? []), .. mcpTools];

        var agent = _chatClient
            .AsAIAgent(options: new ChatClientAgentOptions() {
                ChatOptions = chatOptions,
                AIContextProviders = [_userClaimsProvider],
                Name = "DexOtpAgent"
            });
        // Execute only the send leg now; OTP code collection is done by the workflow host via RequestPort.
        var sendPrompt = _prompts.Render(nameof(AgentsConstants.PromptDefaults.OtpCodeSenderPrompt), new {
            channel = sms ? "Sms" : "Email",
            phoneNumber = sms ? caseData.PhoneNumber : null,
            email = sms ? null : caseData.Email,
            securityToken
        });
        await agent.RunAsync<string>(sendPrompt, cancellationToken: cancellationToken);
    }

    private static string MaskPhone(string? phone) {
        if (string.IsNullOrWhiteSpace(phone)) {
            return "your registered phone";
        }
        var digits = System.Text.RegularExpressions.Regex.Replace(phone, @"\D", "");
        return digits.Length < 4 ? "your registered phone" : $"***{digits[^4..]}";
    }

    private static string MaskEmail(string? email) {
        if (string.IsNullOrWhiteSpace(email)) {
            return "your registered email";
        }

        var at = email.IndexOf('@');
        return at < 1 ? "your registered email" : $"{email[0]}***{email[at..]}";
    }
}
