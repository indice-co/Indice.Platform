using System.Text.Json.Nodes;
using Indice.Features.Agents.Core.Models.Cases;
using Indice.Features.Agents.Core.Services.Cases;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI.Workflows;

namespace Indice.Features.Agents.Core.Workflows.Steps.Cases;

/// <summary>
/// Step 1 of the Cases workflow: Retrieves case data from external API/MCP based on user input.
/// Extracts phone and email for OTP delivery, and identifies the ownership verification field.
/// </summary>
public sealed class CaseDataRetriever : Executor<CasesWorkflowState, CaseRetrievalOutput>
{
    private readonly IExternalCaseApiService _caseApiService;

    /// <summary>Creates a new <see cref="CaseDataRetriever"/>.</summary>
    public CaseDataRetriever(IExternalCaseApiService caseApiService) : base(nameof(CaseDataRetriever))
    {
        _caseApiService = caseApiService ?? throw new ArgumentNullException(nameof(caseApiService));
    }

    /// <inheritdoc/>
    public override async ValueTask<CaseRetrievalOutput> HandleAsync(
        CasesWorkflowState state,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        if (state is null)
            throw new ArgumentNullException(nameof(state));

        // Extract the user's query from the chat message
        var userInput = state.Message.Text ?? string.Empty;

        // Retrieve case data via external API/MCP
        var caseData = await _caseApiService.RetrieveCaseAsync(
            userInput,
            state.UserIdentifier,
            cancellationToken);

        if (caseData is null)
        {
            throw new InvalidOperationException("Case data retrieval returned null.");
        }

        // Extract required fields with fallback handling
        var caseId = caseData["CaseId"]?.GetValue<string>() ?? throw new InvalidOperationException("CaseId not found in case data.");
        var phoneNumber = caseData["PhoneNumber"]?.GetValue<string>();
        var email = caseData["Email"]?.GetValue<string>();

        // Validate that at least one contact method is available
        if (string.IsNullOrWhiteSpace(phoneNumber) && string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("No phone number or email found in case data for OTP delivery.");
        }

        return new CaseRetrievalOutput(
            CaseData: caseData,
            CaseId: caseId,
            UserIdentifier: state.UserIdentifier,
            PhoneNumber: phoneNumber,
            Email: email);
    }
}
