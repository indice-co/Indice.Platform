namespace Indice.Features.Identity.SignInLogs.Models;

/// <summary>Data structure for issued tokens.</summary>
public class SignInLogEntryToken
{
    /// <summary>Gets the type of the token.</summary>
    public string TokenType { get; set; }
    /// <summary>Gets the token value.</summary>
    public string TokenValue { get; set; }
}
