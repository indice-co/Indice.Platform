namespace Indice.Features.Media.AspNetCore.Models.Requests;

/// <summary>The response model after a file is uploaded.</summary>

/// <summary>
/// Represents the The folder created response with the folder Id.
/// </summary>
/// <param name="FileId">The file Id for the media file created</param>
public record UploadFileResponse(Guid FileId);
