using System.Text.Json;

namespace Indice.Features.Agents.Core.Models;

/// <summary>
/// Customer data retrieved from an external system for a single <see cref="ExternalReference"/>.
/// The payload is treated as undisclosed until the person on the other end of the conversation has been
/// verified against it; only then is it rendered as a card.
/// </summary>
public class CustomerDataRecord
{
    /// <summary>The reference the payload was retrieved for.</summary>
    public required ExternalReference Reference { get; init; }

    /// <summary>The payload shape, defaulting to <see cref="ExternalReference.Type"/>. Selects the card template.</summary>
    public string DataType { get; init; } = string.Empty;

    /// <summary>The raw JSON payload as returned by the external system.</summary>
    public required JsonElement Data { get; init; }
}
