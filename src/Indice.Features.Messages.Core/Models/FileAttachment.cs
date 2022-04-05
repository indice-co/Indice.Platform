using Indice.Extensions;

namespace Indice.Features.Messages.Core.Models
{
    /// <summary>
    /// Models a file attachment.
    /// </summary>
    public class FileAttachment
    {
        private readonly Func<Stream> _streamAccessor;

        /// <summary>
        /// Creates a new instance of <see cref="FileAttachment"/>.
        /// </summary>
        public FileAttachment() {
            Id = Guid.NewGuid();
            Guid = Guid.NewGuid();
        }

        /// <summary>
        /// Creates a new instance of <see cref="FileAttachment"/>.
        /// </summary>
        /// <param name="streamAccessor">Provides a delegate for reading the file stream.</param>
        public FileAttachment(Func<Stream> streamAccessor) : this() => _streamAccessor = streamAccessor;

        /// <summary>
        /// Creates a new instance of <see cref="FileAttachment"/>.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        public FileAttachment(string fileName) : this() => PopulateFrom(fileName, null, false);

        /// <summary>
        /// Creates a new instance of <see cref="FileAttachment"/>.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="stream">The file stream.</param>
        /// <param name="saveData">Decides whether to write stream contents to <see cref="Data"/> property.</param>
        public FileAttachment(string fileName, Stream stream, bool saveData = false) : this() {
            _streamAccessor = () => stream;
            PopulateFrom(fileName, stream, saveData);
        }

        /// <summary>
        /// Creates a new instance of <see cref="FileAttachment"/>.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="streamAccessor">Provides a delegate for reading the file stream.</param>
        /// <param name="saveData">Decides whether to write stream contents to <see cref="Data"/> property.</param>
        public FileAttachment(string fileName, Func<Stream> streamAccessor, bool saveData = false) {
            _streamAccessor = streamAccessor;
            PopulateFrom(fileName, null, saveData);
        }

        /// <summary>
        /// The unique id of the file.
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// The unique id of the file, for internal use.
        /// </summary>
        public Guid Guid { get; set; }
        /// <summary>
        /// The name of the file.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The file extension.
        /// </summary>
        public string FileExtension { get; set; }
        /// <summary>
        /// The file mime type.
        /// </summary>
        public string ContentType { get; set; }
        /// <summary>
        /// The file size.
        /// </summary>
        public int ContentLength { get; set; }
        /// <summary>
        /// The file as a byte array.
        /// </summary>
        public byte[] Data { get; set; }
        /// <summary>
        /// The file URI.
        /// </summary>
        public string Uri => $"{Guid.ToString("N")[..2]}/{Guid:N}{FileExtension}";

        /// <summary>
        /// 
        /// </summary>
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
