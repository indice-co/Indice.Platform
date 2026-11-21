using System.Text.Json.Serialization;

namespace Indice.Features.Agents.Core.Models;

/// <summary>
/// A downloadable file attached to a chat message, such as a generated report. Sent to the client as a message part of type
/// <see cref="AgentsConstants.MediaTypes.File"/>; the client downloads the bytes from
/// <c>GET my/chats/{chatId}/files/{id}</c>.
/// </summary>
public class FileReference
{
    /// <summary>The file id.</summary>
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    /// <summary>The name the file is downloaded as.</summary>
    [JsonPropertyName("fileName")]
    public string FileName { get; init; } = string.Empty;

    /// <summary>The media type of the file.</summary>
    [JsonPropertyName("contentType")]
    public string ContentType { get; init; } = string.Empty;

    /// <summary>The size in bytes.</summary>
    [JsonPropertyName("length")]
    public long Length { get; init; }

    /// <summary>Lower-case hex SHA-256 of the content, so the reader can verify the downloaded file.</summary>
    [JsonPropertyName("sha256")]
    public string Sha256 { get; init; } = string.Empty;
}

/// <summary>A stored file together with its bytes.</summary>
/// <param name="Reference">The file's metadata.</param>
/// <param name="Data">The content.</param>
/// <param name="CreatedAt">When the file was saved.</param>
public sealed record StoredFile(FileReference Reference, byte[] Data, DateTimeOffset CreatedAt);
