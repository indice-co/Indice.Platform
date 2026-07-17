using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Workflows.Abstractions;

/// <summary>Entry point for executing the Dex RAG pipeline against a single user question.</summary>
public interface IDexChatClient : IChatClient
{
}
