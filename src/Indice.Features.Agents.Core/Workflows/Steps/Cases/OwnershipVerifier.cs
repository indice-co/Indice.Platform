using Indice.Features.Agents.Core.Models.Cases;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Microsoft.Agents.AI.Workflows;

namespace Indice.Features.Agents.Core.Workflows.Steps.Cases;

/// <summary>
/// Step 2 of the Cases workflow: Requests user to verify ownership of the case by confirming a specific field.
/// Uses a prompt template to generate the verification request with the field name and masked value.
/// </summary>
public sealed class OwnershipVerifier : Executor<CaseRetrievalOutput, OwnershipVerificationOutput>
{
    private readonly IPromptTemplateRenderer _promptRenderer;

    /// <summary>Creates a new <see cref="OwnershipVerifier"/>.</summary>
    public OwnershipVerifier(IPromptTemplateRenderer promptRenderer) : base(nameof(OwnershipVerifier))
    {
        _promptRenderer = promptRenderer ?? throw new ArgumentNullException(nameof(promptRenderer));
    }

    /// <inheritdoc/>
    public override async ValueTask<OwnershipVerificationOutput> HandleAsync(
        CaseRetrievalOutput caseData,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        if (caseData is null)
            throw new ArgumentNullException(nameof(caseData));

        // Extract the ownership verification field name and value from case data
        // The field name is determined by the prompt configuration for this case type
        var verificationFieldName = "please Confrim your license plate";

        var verificationFieldValue = caseData.VerificationValue 
            ?? throw new InvalidOperationException($"Verification field '{verificationFieldName}' not found in case data.");

        // Mask the field value for security (last 4 chars or email domain masking)
        var maskedValue = MaskFieldValue(verificationFieldValue, verificationFieldName);

        // Render the verification prompt using template
        var verificationPrompt = _promptRenderer.Render("CasesOwnershipVerifier", new
        {
            fieldName = verificationFieldName,
            maskedValue = maskedValue
        });

        return new OwnershipVerificationOutput(
            CaseRetrievalData: caseData,
            VerificationFieldName: verificationFieldName,
            VerificationFieldValue: verificationFieldValue,
            VerificationPrompt: verificationPrompt);
    }

    /// <summary>
    /// Masks sensitive field values for display to user.
    /// </summary>
    private static string MaskFieldValue(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "****";

        return fieldName.ToLower() switch
        {
            "email" => MaskEmail(value),
            "phonenumber" or "phone" => MaskPhone(value),
            "ssn" or "socialsecuritynumber" => MaskSSN(value),
            "cardnumber" or "creditcard" => MaskCardNumber(value),
            _ => MaskLastFourDigits(value)
        };
    }

    private static string MaskEmail(string email)
    {
        var parts = email.Split('@');
        if (parts.Length != 2)
            return "****";
        var name = parts[0];
        var domain = parts[1];
        var namePart = name.Length > 2 ? $"{name[0]}***{name[^1]}" : "****";
        var domainPart = domain.Length > 4 ? $"{domain[..2]}*****.{domain.Split('.')[^1]}" : domain;
        return $"{namePart}@{domainPart}";
    }

    private static string MaskPhone(string phone)
    {
        var digits = System.Text.RegularExpressions.Regex.Replace(phone, @"\D", "");
        if (digits.Length < 4)
            return "****";
        return $"+1****{digits[^4..]}";
    }

    private static string MaskSSN(string ssn)
    {
        var digits = System.Text.RegularExpressions.Regex.Replace(ssn, @"\D", "");
        if (digits.Length < 4)
            return "****";
        return $"***-**-{digits[^4..]}";
    }

    private static string MaskCardNumber(string card)
    {
        var digits = System.Text.RegularExpressions.Regex.Replace(card, @"\D", "");
        if (digits.Length < 4)
            return "****";
        return $"****-****-****-{digits[^4..]}";
    }

    private static string MaskLastFourDigits(string value)
    {
        var digits = System.Text.RegularExpressions.Regex.Replace(value, @"\D", "");
        if (digits.Length < 4)
            return "****";
        return $"***{digits[^4..]}";
    }
}
