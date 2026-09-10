using Azure.AI.OpenAI;
using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Workflows.Prompts;
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
public sealed class OtpCodeSendStep : Executor<UserInputValidationOutput, OtpChallengeOutput>
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
    public override async ValueTask<OtpChallengeOutput> HandleAsync(
        UserInputValidationOutput validationData,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {

        ArgumentNullException.ThrowIfNull(validationData);
        var caseData = validationData.OwnershipVerificationData.CaseRetrievalData;
        var maskedPhoneNumber = MaskPhone(caseData.PhoneNumber);

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
            phoneNumber =  caseData.PhoneNumber,
            email = caseData.Email,
            securityToken = caseData.CaseId
        });
        _ = await agent.RunAsync<string>(sendPrompt, cancellationToken: cancellationToken);
        var otpPrompt = _messageLocalizer.OtpVerificationCodeSendMessage(maskedPhoneNumber);
        return new OtpChallengeOutput(
            ValidationData: validationData,
            //TODO: Support dual Phone /Email OTP delivery. For now, we only support phone delivery.
            Prompt: otpPrompt,
            PhoneNumber: caseData.PhoneNumber,
            Email: caseData.Email,
            CaseId: caseData.CaseId,
            FailedAttempts: 0,
            MaxFailedAttempts: _options.CasesWorkflow.MaxOtpValidationAttempts);
    }

    private static string MaskPhone(string? phone) {
        if (string.IsNullOrWhiteSpace(phone)) {
            return "your registered phone";
        }
        var digits = System.Text.RegularExpressions.Regex.Replace(phone, @"\D", "");
        return digits.Length < 4 ? "your registered phone" : $"***{digits[^4..]}";
    }
}
/// <summary>
/// Output from the OTP send step. Represents a pending OTP challenge that requires user input.
/// </summary>
/// <param name="ValidationData">The upstream validated ownership data.</param>
/// <param name="Prompt">Prompt shown to the user asking for OTP input.</param>
/// <param name="PhoneNumber">Phone number used for OTP delivery.</param>
/// <param name="Email">Email used for OTP delivery when applicable.</param>
/// <param name="CaseId">Case id associated with this challenge.</param>
/// <param name="FailedAttempts">Number of invalid OTP attempts already made.</param>
/// <param name="MaxFailedAttempts">Maximum invalid OTP attempts allowed before failing.</param>
public record OtpChallengeOutput(
    UserInputValidationOutput ValidationData,
    string Prompt,
    string? PhoneNumber,
    string? Email,
    string CaseId,
    int FailedAttempts = 0,
    int MaxFailedAttempts = 2);
