using Indice.Extensions;

namespace Indice.Features.Media.AspNetCore.Models.Commands;
/// <summary> The command used to upload a file. </summary>
/// <remarks>Constructs a new <see cref="UploadFileCommand"/></remarks>
/// <param name="streamAccessor">The file stream accessor.</param>
/// <param name="fileName"></param>
/// <param name="fileExtension"></param>
/// <param name="contentType"></param>
/// <param name="contentLength"></param>
public class UploadFileCommand(Func<Stream> streamAccessor, string fileName, int contentLength, string? fileExtension = null, string? contentType = null)
{
    /// <summary> The file's Id. </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary> Id for internal usage. </summary>
    public Guid Guid { get; set; } = Guid.NewGuid();
    /// <summary> The file's name. </summary>
    public string Name { get; set; } = Path.GetFileName(fileName);
    /// <summary> The file's extension. </summary>
    public string FileExtension { get; set; } = fileExtension ?? Path.GetExtension(fileName);
    /// <summary> The file's content type. </summary>
    public string ContentType { get; set; } = contentType ?? FileExtensions.GetMimeType(Path.GetFileName(fileName));
    /// <summary> The file's content length. </summary>
    public int ContentLength { get; set; } = contentLength;
    /// <summary> The byte array of file's data. </summary>
    public byte[]? Data { get; set; }
    /// <summary> The parent folder's Id. </summary>
    public Guid? FolderId { get; set; }
    /// <summary> The file's uri. </summary>
    public string Uri => $"{Guid.ToString("N")[..2]}/{Guid:N}{FileExtension}";
    /// <summary> The file stream. </summary>
    public Stream OpenReadStream() => streamAccessor();
}
