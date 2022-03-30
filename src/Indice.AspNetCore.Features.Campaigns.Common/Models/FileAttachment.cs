using Indice.Extensions;

namespace Indice.AspNetCore.Features.Campaigns.Models
{
    public class FileAttachment
    {
        private readonly Func<Stream> _streamAccessor;

        public FileAttachment() {
            Id = Guid.NewGuid();
            Guid = Guid.NewGuid();
        }

        public FileAttachment(Func<Stream> streamAccessor) : this() => _streamAccessor = streamAccessor;

        public FileAttachment(string fileName) : this() => PopulateFrom(fileName, null, false);

        public FileAttachment(string fileName, Stream stream, bool saveData = false) : this() {
            _streamAccessor = () => stream;
            PopulateFrom(fileName, stream, saveData);
        }

        public FileAttachment(string fileName, Func<Stream> streamAccessor, bool saveData = false) {
            _streamAccessor = streamAccessor;
            PopulateFrom(fileName, null, saveData);
        }

        public Guid Id { get; set; }
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public string FileExtension { get; set; }
        public string ContentType { get; set; }
        public int ContentLength { get; set; }
        public byte[] Data { get; set; }
        public string Uri => $"{Guid.ToString("N").Substring(0, 2)}/{Guid:N}{FileExtension}";

        public Stream OpenReadStream() => _streamAccessor();

        private void PopulateFrom(string fileName, Stream stream, bool saveData = false) {
            Name = Path.GetFileName(fileName);
            FileExtension = Path.GetExtension(fileName);
            ContentType = FileExtensions.GetMimeType(Path.GetExtension(fileName));
            if (stream is null) {
                return;
            }
            ContentLength = (int)stream.Length;
            if (saveData) {
                stream.Seek(0, SeekOrigin.Begin);
                using (var memoryStream = new MemoryStream()) {
                    stream.CopyTo(memoryStream);
                    Data = memoryStream.ToArray();
                }
                stream.Seek(0, SeekOrigin.Begin);
            }
        }
    }
}
