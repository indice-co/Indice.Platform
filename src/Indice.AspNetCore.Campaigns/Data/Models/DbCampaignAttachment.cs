using System;
using System.IO;
using Indice.Extensions;

namespace Indice.AspNetCore.Features.Campaigns.Data.Models
{
    public class DbCampaignAttachment
    {
        public DbCampaignAttachment() {
            Id = Guid.NewGuid();
            Guid = Guid.NewGuid();
        }

        public DbCampaignAttachment(string fileName) : this() => PopulateFrom(fileName, null, false);

        public DbCampaignAttachment(string fileName, Stream dataStream, bool saveData = false) : this() => PopulateFrom(fileName, dataStream, saveData);

        public Guid Id { get; set; }
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public string FileExtension { get; set; }
        public string ContentType { get; set; }
        public int ContentLength { get; set; }
        public byte[] Data { get; set; }
        public string Uri => $"{Guid.ToString("N").Substring(0, 2)}/{Guid:N}{FileExtension}";

        public void PopulateFrom(string fileName, Stream dataStream, bool saveData = false) {
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