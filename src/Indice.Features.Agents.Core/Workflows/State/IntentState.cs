using Indice.Features.Agents.Core.Models;

namespace Indice.Features.Agents.Core.Workflows.State;

/// <summary>Immutable read-only context carried alongside the typed payload through every pipeline edge.</summary>
public record IntentState(Intent Intent, RetrievalFilters Filters);
