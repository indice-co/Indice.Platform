namespace Indice.Features.Agents.Core.Models;

/// <summary>Author role of a chat message exposed at the service boundary. Mirrors <see cref="ChatMessageRole"/>.</summary>
public enum ChatRole
{
    /// <summary>End-user message.</summary>
    User = 0,

    /// <summary>Assistant (LLM) response.</summary>
    Assistant = 1,

    /// <summary>System / instructions message.</summary>
    System = 2,
}
