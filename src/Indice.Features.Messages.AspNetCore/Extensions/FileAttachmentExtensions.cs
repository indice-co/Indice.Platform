using System.IO;
using Indice.Features.Messages.Core.Models;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Messages.AspNetCore.Extensions
{
    /// <summary>
    /// Extension methods on <see cref="FileAttachment"/> type.
    /// </summary>
    public static class FileAttachmentExtensions
    {
        /// <summary>
        /// Populates an instance of <see cref="FileAttachment"/> from an <see cref="IFormFile"/>.
        /// </summary>
        /// <param name="fileAttachment">The instance of <see cref="FileAttachment"/>.</param>
        /// <param name="file">The <see cref="IFormFile"/> instance.</param>
        /// <param name="saveData">Copy file bytes to <see cref="FileAttachment.Data"/> property.</param>
        public static FileAttachment PopulateFrom(this FileAttachment fileAttachment, IFormFile file, bool saveData = false) {
            fileAttachment.Name = Path.GetFileName(file.FileName);
            fileAttachment.FileExtension = Path.GetExtension(file.FileName);
            fileAttachment.ContentLength = (int)file.Length;
            fileAttachment.ContentType = file.ContentType;
            if (saveData && file.Length > 0) {
                using (var inputStream = file.OpenReadStream()) {
                    using (var memoryStream = new MemoryStream()) {
                        inputStream.CopyTo(memoryStream);
                        fileAttachment.Data = memoryStream.ToArray();
                    }
                }
            }
            return fileAttachment;
        }
    }
}
