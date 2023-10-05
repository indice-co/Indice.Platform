namespace Indice.Features.Media.AspNetCore.Models.Commands;
/// <summary> The command used to upload a file. </summary>
public class UploadFileCommand {

    private readonly Func<Stream> _streamAccessor;
    /// <summary>Constructs a new <see cref="UploadFileCommand"/></summary>
    /// <param name="streamAccessor">The file stream accessor.</param>
    public UploadFileCommand(Func<Stream> streamAccessor) {
        Id = Guid.NewGuid();
        Guid = Guid.NewGuid();
        _streamAccessor = streamAccessor;
    }
    /// <summary> The file's Id. </summary>
    public Guid Id { get; set; }
    /// <summary> Id for internal usage. </summary>
    public Guid Guid { get; set; }
    /// <summary> The file's name. </summary>
    public string Name { get; set; }
    /// <summary> The file's extension. </summary>
    public string FileExtension { get; set; }
    /// <summary> The file's content type. </summary>
    public string ContentType { get; set; }
    /// <summary> The file's content length. </summary>
    public int ContentLength { get; set; }
    /// <summary> The byte array of file's data. </summary>
    public byte[]? Data { get; set; }
    /// <summary> The parent folder's Id. </summary>
    public Guid? FolderId { get; set; }
    /// <summary> The file's uri. </summary>
    public string Uri => $"{Guid.ToString("N")[..2]}/{Guid:N}{FileExtension}";
    /// <summary> The file stream. </summary>
    public Stream OpenReadStream() => _streamAccessor();
}
