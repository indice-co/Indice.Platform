using System.ComponentModel;
using System.Security.Claims;
using Indice.Features.Agents.Core.Workflows;
using Indice.Features.Agents.Core.Workflows.Abstractions;
using ModelContextProtocol.Server;

namespace Indice.Features.Agents.Server.Mcp;

/// <summary>MCP tool surface backed by the Dex RAG pipeline.</summary>
[McpServerToolType]
public sealed class DexMcpTools(IDexRunner runner)
{
    /// <summary>Answers a natural-language question against the knowledge base.</summary>
    [McpServerTool(Name = "query_knowledge_base"),
     Description("Query the Dex knowledge base with a natural-language question. Returns a grounded answer with source citations.")]
    public async Task<string> QueryKnowledgeBaseAsync(
        [Description("The natural-language question to answer.")] string question,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var user = principal.Identity.Name;
        var result = await runner.RunAsync(new RagRequest { Question = question }, cancellationToken);

        if (result.Failed) {
            return $"[pipeline error] {result.FailureReason}";
        }

        if (result.Citations.Count == 0) {
            return result.Answer ?? "(no answer)";
        }

        var citations = string.Join("\n", result.Citations.Select((c, i) => $"[{i + 1}] {c.Title} — {c.DocumentId}"));
        return $"{result.Answer}\n\nSources:\n{citations}";
    }
}