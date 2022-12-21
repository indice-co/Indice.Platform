using Indice.Extensions;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Cases.Data.Models
{
    public class DbAttachment
    {
        public DbAttachment() {
            Id = Guid.NewGuid();
            Guid = Guid.NewGuid();
        }
        public DbAttachment(string fileName) : this() => PopulateFrom(fileName, CaseId, null, false);

        public DbAttachment(string fileName, Stream dataStream, bool saveData = false) : this() => PopulateFrom(fileName, CaseId, dataStream, saveData);

        public Guid Id { get; set; }
        public DateTimeOffset LastModified { get; set; }
        public Guid CaseId { get; set; }
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public string FileExtension { get; set; }
        public string ContentType { get; set; }
        public long ContentLength { get; set; }
        public byte[] Data { get; set; }
        public virtual DbCase Case { get; set; }
        public string Uri => $"{Guid.ToString("N").Substring(0, 2)}/{Guid:N}{FileExtension}";

        public void PopulateFrom(IFormFile file, Guid caseId, bool saveData = false) {
            CaseId = caseId;
            Name = Path.GetFileName(file.FileName);
            FileExtension = Path.GetExtension(file.FileName);
            ContentLength = (int)file.Length;
            ContentType = file.ContentType;
            if (saveData && file.Length > 0) {
                using (var inputStream = file.OpenReadStream()) {
                    using (var memoryStream = new MemoryStream()) {
                        inputStream.CopyTo(memoryStream);
                        Data = memoryStream.ToArray();
                    }
                }
            }
        }

        public void PopulateFrom(string fileName, Guid caseId, Stream dataStream, bool saveData = false) {
            CaseId = caseId;
            Name = Path.GetFileName(fileName);
            FileExtension = Path.GetExtension(fileName);
            ContentType = FileExtensions.GetMimeType(Path.GetExtension(fileName));
            if (dataStream is null) {
                return;
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
}