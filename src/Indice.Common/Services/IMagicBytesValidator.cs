namespace Indice.Services;

/// <summary>Service that validates uploaded files by reading their magic bytes (file signatures).</summary>
public interface IMagicBytesValidator
{
    /// <summary>Validates that a stream's content matches the expected magic bytes for the given file extension.</summary>
    /// <param name="stream">The file stream to validate. The stream position will be reset to its original position after reading.</param>
    /// <param name="fileExtension">The file extension (e.g. ".jpg", ".png").</param>
    /// <returns>
    /// <see langword="true"/> if the file content matches the expected magic bytes for the extension,
    /// or if the extension does not have a known signature; otherwise <see langword="false"/>.
    /// </returns>
    Task<bool> IsValidAsync(Stream stream, string fileExtension);
}

/// <summary>Default implementation of <see cref="IMagicBytesValidator"/> that validates files by checking their magic bytes (file signatures).</summary>
public class MagicBytesValidator : IMagicBytesValidator
{
    // Each entry maps a file extension to one or more valid leading-byte sequences.
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<byte[]>> Signatures =
        new Dictionary<string, IReadOnlyList<byte[]>>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"]  = [[ 0xFF, 0xD8, 0xFF ]],
            [".jpeg"] = [[ 0xFF, 0xD8, 0xFF ]],
            [".png"]  = [[ 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A ]],
            [".gif"]  =
            [
                [ 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 ], // GIF87a
                [ 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 ]  // GIF89a
            ],
            [".bmp"]  = [[ 0x42, 0x4D ]],                       // BM
            [".pdf"]  = [[ 0x25, 0x50, 0x44, 0x46 ]],           // %PDF
            [".zip"]  = [[ 0x50, 0x4B, 0x03, 0x04 ]],
            [".docx"] = [[ 0x50, 0x4B, 0x03, 0x04 ]],           // OOXML (ZIP)
            [".xlsx"] = [[ 0x50, 0x4B, 0x03, 0x04 ]],           // OOXML (ZIP)
            [".pptx"] = [[ 0x50, 0x4B, 0x03, 0x04 ]],           // OOXML (ZIP)
        };

    /// <inheritdoc />
    public async Task<bool> IsValidAsync(Stream stream, string fileExtension) {
        if (string.IsNullOrWhiteSpace(fileExtension)) {
            return true;
        }
        if (!fileExtension.StartsWith('.')) {
            fileExtension = '.' + fileExtension;
        }

        // WebP requires a compound check: "RIFF" at offset 0 and "WEBP" at offset 8.
        if (fileExtension.Equals(".webp", StringComparison.OrdinalIgnoreCase)) {
            return await IsValidWebPAsync(stream);
        }

        // SVG is a text-based XML format with no binary magic bytes.
        if (fileExtension.Equals(".svg", StringComparison.OrdinalIgnoreCase)) {
            return await IsValidSvgAsync(stream);
        }

        if (!Signatures.TryGetValue(fileExtension, out var signatures)) {
            // Extension not in our dictionary – we cannot validate, so allow it.
            return true;
        }

        var maxLength = signatures.Max(s => s.Length);
        var buffer = new byte[maxLength];
        var originalPosition = stream.CanSeek ? stream.Position : 0L;
        var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, maxLength));
        if (stream.CanSeek) {
            stream.Seek(originalPosition, SeekOrigin.Begin);
        }
        var actualBytes = buffer.AsSpan(0, bytesRead);
        foreach (var sig in signatures) {
            if (actualBytes.Length >= sig.Length && actualBytes[..sig.Length].SequenceEqual(sig)) {
                return true;
            }
        }
        return false;
    }

    private static async Task<bool> IsValidWebPAsync(Stream stream) {
        // WebP structure: bytes 0-3 = "RIFF", bytes 4-7 = file size, bytes 8-11 = "WEBP"
        var buffer = new byte[12];
        var originalPosition = stream.CanSeek ? stream.Position : 0L;
        var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, 12));
        if (stream.CanSeek) {
            stream.Seek(originalPosition, SeekOrigin.Begin);
        }
        if (bytesRead < 12) {
            return false;
        }
        ReadOnlySpan<byte> riff = [0x52, 0x49, 0x46, 0x46]; // "RIFF"
        ReadOnlySpan<byte> webp = [0x57, 0x45, 0x42, 0x50]; // "WEBP"
        return buffer.AsSpan(0, 4).SequenceEqual(riff) && buffer.AsSpan(8, 4).SequenceEqual(webp);
    }

    private static async Task<bool> IsValidSvgAsync(Stream stream) {
        // Read enough bytes to detect <?xml or <svg, accounting for optional UTF-8 BOM.
        var buffer = new byte[256];
        var originalPosition = stream.CanSeek ? stream.Position : 0L;
        var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));
        if (stream.CanSeek) {
            stream.Seek(originalPosition, SeekOrigin.Begin);
        }
        // Skip UTF-8 BOM (EF BB BF) if present.
        var start = (bytesRead >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF) ? 3 : 0;
        var content = System.Text.Encoding.UTF8.GetString(buffer, start, bytesRead - start).TrimStart();
        return content.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase)
            || content.StartsWith("<svg", StringComparison.OrdinalIgnoreCase);
    }
}
