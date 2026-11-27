using Indice.Features.Cases.Core.Models;

namespace Indice.Features.Cases.Core.Services.Abstractions;

/// <summary>Trusted scanner integration; do not expose this service to untrusted callers.</summary>
public interface ICaseAttachmentScanService
{
    /// <summary>Sets the scan status for an attachment. Returns false if the attachment does not exist.</summary>
    /// <param name="attachmentId">The attachment identifier.</param>
    /// <param name="status">The scanner result.</param>
    /// <param name="scannerId">The identity of the trusted scanner for auditing.</param>
    Task<bool> SetStatus(Guid attachmentId, AttachmentScanStatus status, string scannerId);
}
