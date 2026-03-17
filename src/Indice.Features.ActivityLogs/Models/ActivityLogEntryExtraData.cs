using System.Text.Json.Nodes;
using Indice.Features.Identity.Core;

namespace Indice.Features.ActivityLogs.Models;

/// <summary>Additional information about the user's activity log entry.</summary>
public class ActivityLogEntryExtraData
{
    /// <summary>Additional information about the user's activity log entry.</summary>
    public JsonNode? ExtraData { get; set; }
}
