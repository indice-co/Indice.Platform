namespace Indice.Features.Agents.Core.Models;

/// <summary>Author role of a chat session message.</summary>
public enum ChatMessageRole
{
    /// <summary>End-user message.</summary>
    User = 0,

    /// <summary>Assistant (LLM) response.</summary>
    Assistant = 1,

    /// <summary>System / instructions message.</summary>
    System = 2,

    /// <summary>Tool call or tool result.</summary>
    Tool = 3,
}
