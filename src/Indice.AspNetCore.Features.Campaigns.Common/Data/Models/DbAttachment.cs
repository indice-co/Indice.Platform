namespace Indice.AspNetCore.Features.Campaigns.Data.Models
{
    public class DbAttachment
    {
        public DbAttachment() {
            Id = Guid.NewGuid();
            Guid = Guid.NewGuid();
        }

        public Guid Id { get; set; }
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public string FileExtension { get; set; }
        public string ContentType { get; set; }
        public int ContentLength { get; set; }
        public byte[] Data { get; set; }
        public string Uri { get; set; }
    }
}
