using System.Text.Json.Serialization;

namespace Indice.Features.Cases.Core.Models;

/// <summary>Define the status for the customer. It is defined at the <see cref="CheckpointType.Status"/>.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))] // unfortunately, Elsa uses Newtonsoft.Json and overwrites our Converters...
public enum CaseStatus : short
{
    /// <summary>Submitted.</summary>
    Submitted,
    /// <summary>InProgress.</summary>
    InProgress,
    /// <summary>Completed.</summary>
    Completed,
    /// <summary>Deleted.</summary>
    Deleted
}