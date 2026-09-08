using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Extensions;

/// <summary>
/// Helpers around <see cref="DataContent"/>: creation, transformations etc.
/// </summary>
public static class DataContentExtensions
{
    /// <summary>Serializes a payload into the atomic <see cref="DataContent"/> part its media type stands for.</summary>
    public static DataContent JsonPart<TPayload>(TPayload payload, string mediaType, string? name=null)
        => new(JsonSerializer.SerializeToUtf8Bytes(payload), mediaType) { Name = name };

}
