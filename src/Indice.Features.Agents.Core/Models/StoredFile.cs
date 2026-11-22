namespace Indice.Features.Agents.Core.Models;

/// <summary>A stored file together with its bytes.</summary>
/// <param name="Reference">The file's metadata.</param>
/// <param name="Data">The content.</param>
/// <param name="CreatedAt">When the file was saved.</param>
public sealed record StoredFile(FileReference Reference, byte[] Data, DateTimeOffset CreatedAt);
