namespace Indice.Features.Agents.Core.Workflows;

/// <summary>Output payload of <c>IntentClassifier</c>.</summary>
public class IntentOutput
{
    /// <summary>The classified intent.</summary>
    public Intent Intent { get; init; } = new();

    /// <summary>Retrieval filters derived from the intent (category, language).</summary>
    public RetrievalFilters Filters { get; init; } = new();
}
