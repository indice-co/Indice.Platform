using System.Text.Json.Nodes;

namespace Indice.Features.Agents.Core.Workflows.State;

public class OperatorState
{
    public JsonNode CaseData { get; set; }
    public string CaseId { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? VerificationValue { get; set; }
}
