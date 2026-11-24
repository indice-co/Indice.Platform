using System.Text.Json.Serialization;

namespace Indice.Features.Cases.Core.Models;

/// <summary>Antivirus scan state of a case attachment.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AttachmentScanStatus
{
    /// <summary>The attachment has not yet been cleared by a scanner.</summary>
    Pending = 0,
    /// <summary>The attachment has been cleared for download.</summary>
    Safe = 1,
    /// <summary>The attachment was found to be unsafe.</summary>
    Unsafe = 2,
    /// <summary>The scan did not complete successfully.</summary>
    ScanFailed = 3
}
