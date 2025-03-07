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
    public DateTimeOffset LastModified { get; set; } = DateTimeOffset.Now;
    public Guid CaseId { get; set; }
    public Guid Guid { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    public string FileExtension { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long ContentLength { get; set; }
    public byte[] Data { get; set; } = null!;
    public virtual DbCase Case { get; set; } = null!;
    public string Uri => $"{Guid.ToString("N")[..2]}/{Guid:N}{FileExtension}";

    public void PopulateFrom(string fileName, Func<Stream> fileStreamAccessor, bool saveData = false) =>
        PopulateFrom(fileName, fileStreamAccessor(), saveData);

    public void PopulateFrom(string fileName, Stream dataStream, bool saveData = false) {
        Name = Path.GetFileName(fileName);
        FileExtension = Path.GetExtension(fileName);
        ContentType = FileExtensions.GetMimeType(Path.GetExtension(fileName));
        if (dataStream is null) {
            ArgumentNullException.ThrowIfNull(dataStream);
        }
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
