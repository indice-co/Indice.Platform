using System.Text.Json;
using Indice.Features.Agents.Core.Services.Risk;

namespace Indice.Features.Agents.Core.Workflows.Steps.Risk;

/// <summary>What the model extracts from the user's request. Every field is optional: absent means the user did not say.</summary>
/// <param name="PlaybookId">The knowledge base playbook the request matches (<c>P-01</c> to <c>P-08</c>), or <c>null</c>.</param>
/// <param name="Tun">The transaction number exactly as the user wrote it.</param>
/// <param name="TransactionDate">The transaction date as <c>yyyy-MM-dd</c>, in the report time zone.</param>
/// <param name="Language">The language the user writes in: <c>el</c> or <c>en</c>.</param>
/// <param name="Purpose">What the report is for, for example a court filing.</param>
public sealed record RiskRequestAnalysis(string? PlaybookId, string? Tun, string? TransactionDate, string? Language, string? Purpose);

/// <summary>A validated request to trace one transaction. Everything in it passed the deterministic checks.</summary>
/// <param name="Tun">The core banking transaction number.</param>
/// <param name="TransactionDate">The date the user gave, <c>yyyy-MM-dd</c>, in the report time zone.</param>
/// <param name="Language">The report language, <c>el</c> or <c>en</c>.</param>
/// <param name="Purpose">What the report is for, or <c>null</c>.</param>
public sealed record RiskTraceRequest(string Tun, string TransactionDate, string Language, string? Purpose);

/// <summary>A request for a report type this agent does not produce yet.</summary>
/// <param name="PlaybookId">The playbook the request matched.</param>
/// <param name="Language">The language to answer in.</param>
public sealed record RiskUnsupportedRequest(string? PlaybookId, string Language);

/// <summary>
/// Everything gathered about one investigation, passed from step to step. <see cref="Results"/> is the query log: every
/// query that ran, in order, and the position of a result in it (counting from one) is the evidence number the report cites
/// as <c>E-n</c>. The type round-trips through JSON because workflow messages are checkpointed.
/// </summary>
public sealed class RiskCase
{
    /// <summary>The request this case answers.</summary>
    public RiskTraceRequest Request { get; set; } = new(string.Empty, string.Empty, "el", null);

    /// <summary>Every executed query, in order. This is the report's annex.</summary>
    public List<RiskQueryResult> Results { get; set; } = [];

    /// <summary>Things the reader must be told that are not rows: an ambiguity in the data, or a fallback that was used.</summary>
    public List<string> Notes { get; set; } = [];

    /// <summary>The evidence number of a result (its position in <see cref="Results"/>, from one), or <c>0</c> when it is not part of the case.</summary>
    public int NumberOf(RiskQueryResult result) => Results.IndexOf(result) + 1;

    /// <summary>The results of a query id, in the order they ran.</summary>
    public IEnumerable<RiskQueryResult> Of(string queryId) => Results.Where(r => string.Equals(r.QueryId, queryId, StringComparison.OrdinalIgnoreCase));

    /// <summary>The first result of a query id, or <c>null</c> when the query did not run.</summary>
    public RiskQueryResult? First(string queryId) => Of(queryId).FirstOrDefault();

    /// <summary>The anchor event: the single row of <c>Q-RISK-01</c>, or <c>null</c> before it is known.</summary>
    public Dictionary<string, JsonElement>? AnchorEvent => First("Q-RISK-01") is { Succeeded: true, Rows: [var row] } ? row : null;
}
