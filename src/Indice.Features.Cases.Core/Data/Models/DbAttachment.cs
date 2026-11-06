using Indice.Extensions;

namespace Indice.Features.Cases.Core.Data.Models;

#pragma warning disable 1591
public class DbAttachment
{
    public DbAttachment() {
    }
    public DbAttachment(Guid caseId) {
        CaseId = caseId;
    }
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;
    public Guid CaseId { get; set; }
    public Guid Guid { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    public string FileExtension { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long ContentLength { get; set; }
    /// <summary>The binary data of the attachment. Left empty (null) when the binary is persisted to a file service.</summary>
    public byte[]? Data { get; set; }
    public virtual DbCase Case { get; set; } = null!;
    public string Uri => $"{Guid.ToString("N")[..2]}/{Guid:N}{FileExtension}";
    /// <summary>The path of the attachment binary when stored in a file service.</summary>
    public string GetPath() => GetPath(Guid, FileExtension);
    /// <summary>The path of the attachment binary when stored in a file service.</summary>
    public static string GetPath(Guid guid, string? fileExtension) => $"attachments/{guid.ToString("N")[..2]}/{guid:N}{fileExtension}";

    public void PopulateFrom(string fileName, Func<Stream> fileStreamAccessor, bool saveData = false) =>
        PopulateFrom(fileName, fileStreamAccessor(), saveData);

    public void PopulateFrom(string fileName, Stream dataStream, bool saveData = false) {
        ArgumentNullException.ThrowIfNull(dataStream);

        Name = Path.GetFileName(fileName);
        FileExtension = Path.GetExtension(fileName);
        ContentType = FileExtensions.GetMimeType(Path.GetExtension(fileName));
        ContentLength = (int)dataStream.Length;
        if (saveData) {
            dataStream.Seek(0, SeekOrigin.Begin);
            using (var memoryStream = new MemoryStream()) {
                dataStream.CopyTo(memoryStream);
                Data = memoryStream.ToArray();
            }
            dataStream.Seek(0, SeekOrigin.Begin);
        }
    }
}
#pragma warning restore 1591
