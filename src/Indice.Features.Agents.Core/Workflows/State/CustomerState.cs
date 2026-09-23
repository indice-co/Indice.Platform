using System.Text.Json;
using System.Text.Json.Nodes;

namespace Indice.Features.Agents.Core.Workflows.State;

/// <summary>
/// Represents customer case data and verification details stored in workflow state.
/// </summary>
public class CustomerState
{
    /// <summary>
    /// Gets or sets the retrieved case data as a JSON payload.
    /// </summary>
    public JsonElement Data { get; set; }

    /// <summary>
    /// Gets or sets the data type of the retrieved case.
    /// </summary>
    public string DataType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the reference identifier of the retrieved case.
    /// </summary>
    public string ReferenceId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the customer's phone number for one-time password delivery, if available.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets the customer's email address for one-time password delivery, if available.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the value used to verify the customer's identity, if available.
    /// </summary>
    public string? ChallengValue { get; set; }

    /// <summary>
    /// Gets or sets the type of verification challenge, if specified.
    /// </summary>
    public string? ChallengeType { get; set; }
}
