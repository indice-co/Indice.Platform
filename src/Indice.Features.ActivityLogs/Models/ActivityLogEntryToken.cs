namespace Indice.Features.ActivityLogs.Models;

/// <summary>Data structure for issued tokens.</summary>
public class ActivityLogEntryToken
{
    /// <summary>Gets the type of the token.</summary>
    public string TokenType { get; set; } = null!;
    /// <summary>Gets the token value.</summary>
    public string TokenValue { get; set; } = null!;
}
