using System;
using Indice.Extensions;

namespace Indice.AspNetCore.Features.Campaigns.Models
{
    public class AttachmentLink
    {
        public Guid Id { get; set; }
        public Guid FileGuid { get; set; }
        public string PermaLink { get; set; }
        public string Label { get; set; }
        public long Size { get; set; }
        public string ContentType { get; set; }
        public string SizeText => Size.ToFileSize();
    }
}