using System.Buffers;
using System.Collections.Frozen;

namespace Indice.Services;

/// <summary>Service that validates uploaded files by reading their magic bytes (file signatures).</summary>
public interface IMagicBytesValidator
{
    /// <summary>Validates that a stream's content matches the expected magic bytes for the given file extension.</summary>
    /// <param name="stream">The file bytes as Stream to validate.</param>
    /// <param name="fileExtension">The file extension (e.g. ".jpg", ".png").</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>
    /// <see langword="true"/> if the file content matches the expected magic bytes for the extension,
    /// or if the extension does not have a known signature; otherwise <see langword="false"/>.
    /// </returns>
    Task<MagicBytesValidationResult> IsValid(Stream stream, string fileExtension, CancellationToken ct = default);
}

/// <summary>
/// Defines how the magic bytes pattern is located within the file buffer.
/// </summary>
public enum MagicBytesCheckStrategy
{
    /// <summary>File begins with the specified bytes.</summary>
    StartsWith,

    /// <summary>File ends with the specified bytes.</summary>
    EndsWith,

    /// <summary>Bytes appear anywhere in the file.</summary>
    Anywhere,

    /// <summary>Any of the candidate byte sequences appear anywhere in the file.</summary>
    AnywhereAnyOf,

    /// <summary>File begins with any of the candidate byte sequences.</summary>
    StartsWithAnyOf,

    /// <summary>File ends with any of the candidate byte sequences.</summary>
    EndsWithAnyOf,

    /// <summary>Bytes appear at a specific offset within the file.</summary>
    Offset
}

/// <summary>
/// Describes a single magic bytes pattern to match against a file buffer.
/// </summary>
/// <param name="Strategy">The matching strategy to apply.</param>
/// <param name="Bytes">The byte pattern to match. Empty when <see cref="Candidates"/> is used.</param>
/// <param name="Offset">Byte offset used when <see cref="Strategy"/> is <see cref="MagicBytesCheckStrategy.Offset"/>.</param>
/// <param name="Candidates">Multiple byte sequences used with AnyOf strategies.</param>
public sealed record MagicBytesSignature(
    MagicBytesCheckStrategy Strategy,
    byte[] Bytes,
    int Offset = 0,
    byte[][]? Candidates = null
);

/// <summary>
/// Represents the result of a magic bytes validation check.
/// </summary>
public sealed record MagicBytesValidationResult
{
    /// <summary>Gets whether the file content matched the expected magic bytes for the given extension.</summary>
    public bool IsValid { get; }

    /// <summary>Gets whether validation was skipped because no signatures are registered for the given extension.</summary>
    public bool IsUnknownExtension { get; }

    /// <summary>Gets the error message when <see cref="IsValid"/> is <see langword="false"/>; otherwise <see langword="null"/>.</summary>
    public string? Error { get; }

    private MagicBytesValidationResult(bool isValid, bool isUnknownExtension, string? error) {
        IsValid = isValid;
        IsUnknownExtension = isUnknownExtension;
        Error = error;
    }

    /// <summary>Returns a successful validation result.</summary>
    public static MagicBytesValidationResult Valid() => new(true, false, null);

    /// <summary>Returns a failed result indicating the extension has no registered signatures.</summary>
    public static MagicBytesValidationResult UnknownExtension(string error) => new(false, true, error);

    /// <summary>Returns a failed result indicating the file content did not match the expected magic bytes.</summary>
    public static MagicBytesValidationResult Failure(string error) => new(false, false, error);

    /// <summary>Allows implicit use in boolean expressions via <see cref="IsValid"/>.</summary>
    public static implicit operator bool(MagicBytesValidationResult result) => result.IsValid;
}

/// <summary>
/// Validates file content against known magic byte signatures to verify the file matches its declared extension.
/// </summary>
public sealed class MagicBytesValidator : IMagicBytesValidator
{
    #region Magic Bytes Signatures
    /// <summary>
    /// Maps file extension aliases to their canonical form.
    /// For example: .jpg -> .jpeg, .tif -> .tiff, .xlsx -> .docx
    /// </summary>
    private static readonly FrozenDictionary<string, string> ExtensionAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
        // Image aliases
        [".jpg"] = ".jpeg",
        [".tif"] = ".tiff",
        [".heif"] = ".heic",
        // Office document aliases (all OOXML formats share ZIP signatures)
        [".xlsx"] = ".docx",
        [".pptx"] = ".docx"
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Maps file extensions to their magic byte signatures for validation.
    /// </summary>
    private static readonly FrozenDictionary<string, MagicBytesSignature[]> ByFileExtension = new Dictionary<string, MagicBytesSignature[]>(StringComparer.OrdinalIgnoreCase) {
        // images
        [".png"] = [new(MagicBytesCheckStrategy.StartsWith, [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a])],
        [".svg"] = [
                new(MagicBytesCheckStrategy.Anywhere,      [0x3C, 0x73, 0x76, 0x67]),
            new(MagicBytesCheckStrategy.EndsWithAnyOf, [], Candidates: [
                [0x3C, 0x2F, 0x73, 0x76, 0x67, 0x3E],
                [0x3C, 0x2F, 0x73, 0x76, 0x67, 0x3E, 0x0A],
                [0x3C, 0x2F, 0x73, 0x76, 0x67, 0x3E, 0x0D, 0x0A],
            ]),
        ],
        [".jpeg"] = [
                new(MagicBytesCheckStrategy.StartsWithAnyOf, [], Candidates: [
                [0xff,0xd8,0xff,0xe0],
                [0xff,0xd8,0xff,0xe1],
                [0xff,0xd8,0xff,0xee],
                [0xff,0xd8,0xff,0xdb],
            ]),
        ],
        [".gif"] = [
                new(MagicBytesCheckStrategy.StartsWithAnyOf, [], Candidates: [
                [0x47,0x49,0x46,0x38,0x37,0x61],
                [0x47,0x49,0x46,0x38,0x39,0x61],
            ]),
        ],
        [".webp"] = [
                new(MagicBytesCheckStrategy.StartsWith, [0x52,0x49,0x46,0x46]),
            new(MagicBytesCheckStrategy.Offset,     [0x57,0x45,0x42,0x50], Offset: 8),
        ],
        [".bmp"] = [new(MagicBytesCheckStrategy.StartsWith, [0x42, 0x4d])],
        [".tiff"] = [
                new(MagicBytesCheckStrategy.StartsWithAnyOf, [], Candidates: [
                [0x49,0x49,0x2a,0x00],
                [0x4d,0x4d,0x00,0x2a],
            ]),
        ],
        [".avif"] = [new(MagicBytesCheckStrategy.Offset, [0x66, 0x74, 0x79, 0x70, 0x61, 0x76, 0x69, 0x66], Offset: 4)],
        [".heic"] = [
            new(MagicBytesCheckStrategy.Offset, [0x66,0x74,0x79,0x70,0x68,0x65,0x69,0x63], Offset: 4),
            new(MagicBytesCheckStrategy.Offset, [0x66,0x74,0x79,0x70,0x68,0x65,0x69,0x78], Offset: 4)
        ],
        [".ico"] = [new(MagicBytesCheckStrategy.StartsWith, [0x00, 0x00, 0x01, 0x00])],

        // documents
        [".pdf"] = [new(MagicBytesCheckStrategy.StartsWith, [0x25, 0x50, 0x44, 0x46])], // %PDF 
        [".doc"] = [new(MagicBytesCheckStrategy.StartsWith, [0xd0, 0xcf, 0x11, 0xe0, 0xa1, 0xb1, 0x1a, 0xe1])],
        [".docx"] = [new(MagicBytesCheckStrategy.StartsWith, [0x50, 0x4b, 0x03, 0x04])], // OOXML (docx/xlsx/pptx) are ZIP-based
        [".ps"] = [new(MagicBytesCheckStrategy.StartsWith, [0x25, 0x21, 0x50, 0x53])], // %!PS
        // archives
        [".zip"] = [
                new(MagicBytesCheckStrategy.StartsWithAnyOf, [], Candidates: [
                [0x50,0x4b,0x03,0x04],
                [0x50,0x4b,0x05,0x06], // empty
                [0x50,0x4b,0x07,0x08], // spanned
            ]),
        ],
        [".rar"] = [
                new(MagicBytesCheckStrategy.StartsWithAnyOf, [], Candidates: [
                [0x52,0x61,0x72,0x21,0x1a,0x07,0x00],      // RAR4
                [0x52,0x61,0x72,0x21,0x1a,0x07,0x01,0x00], // RAR5
            ]),
        ],
        [".7z"] = [new(MagicBytesCheckStrategy.StartsWith, [0x37, 0x7a, 0xbc, 0xaf, 0x27, 0x1c])],
        [".gz"] = [new(MagicBytesCheckStrategy.StartsWith, [0x1f, 0x8b])],
        [".bz2"] = [new(MagicBytesCheckStrategy.StartsWith, [0x42, 0x5a, 0x68])], // BZh
        [".xz"] = [new(MagicBytesCheckStrategy.StartsWith, [0xfd, 0x37, 0x7a, 0x58, 0x5a, 0x00])],
        [".zst"] = [new(MagicBytesCheckStrategy.StartsWith, [0x28, 0xb5, 0x2f, 0xfd])],
        [".tar"] = [
                new(MagicBytesCheckStrategy.Offset, [], Offset: 256, Candidates: [
                [0x75,0x73,0x74,0x61,0x72,0x00,0x30,0x30], // ustar\000
                [0x75,0x73,0x74,0x61,0x72,0x20,0x20,0x00], // ustar  \0
            ]),
        ],

        // audio
        [".mp3"] = [
                new(MagicBytesCheckStrategy.StartsWithAnyOf, [], Candidates: [
                [0xff,0xfb],
                [0xff,0xf3],
                [0xff,0xf2],
                [0x49,0x44,0x33], // ID3
            ])
        ],
        [".ogg"] = [new(MagicBytesCheckStrategy.StartsWith, [0x4f, 0x67, 0x67, 0x53])], // OggS
        [".flac"] = [new(MagicBytesCheckStrategy.StartsWith, [0x66, 0x4c, 0x61, 0x43])], // fLaC
        [".wav"] = [
            new(MagicBytesCheckStrategy.StartsWith, [0x52,0x49,0x46,0x46]),
            new(MagicBytesCheckStrategy.Offset,     [0x57,0x41,0x56,0x45], Offset: 8),
        ],
        [".mid"] = [new(MagicBytesCheckStrategy.StartsWith, [0x4d, 0x54, 0x68, 0x64])], // MThd
        [".aac"] = [
            new(MagicBytesCheckStrategy.StartsWithAnyOf, [], Candidates: [
                [0xff,0xf1],
                [0xff,0xf9],
            ]),
        ],

        // video
        [".mp4"] = [
            new(MagicBytesCheckStrategy.AnywhereAnyOf, [], Candidates: [
                [0x66,0x74,0x79,0x70,0x69,0x73,0x6f,0x6d], // ftypisom
                [0x66,0x74,0x79,0x70,0x6d,0x70,0x34,0x32], // ftypmp42
                [0x66,0x74,0x79,0x70,0x4d,0x53,0x4e,0x56], // ftypMSNV
            ]),
        ],
        [".webm"] = [new(MagicBytesCheckStrategy.StartsWith, [0x1a, 0x45, 0xdf, 0xa3])],
        [".mkv"] = [new(MagicBytesCheckStrategy.StartsWith, [0x1a, 0x45, 0xdf, 0xa3])],
        [".avi"] = [
            new(MagicBytesCheckStrategy.StartsWith, [0x52,0x49,0x46,0x46]),
            new(MagicBytesCheckStrategy.Offset,     [0x41,0x56,0x49,0x20], Offset: 8),
        ],
        [".wmv"] = [new(MagicBytesCheckStrategy.StartsWith, [0x30, 0x26, 0xb2, 0x75, 0x8e, 0x66, 0xcf, 0x11])],
        [".3gp"] = [new(MagicBytesCheckStrategy.Offset, [0x66, 0x74, 0x79, 0x70, 0x33, 0x67], Offset: 4)],
        [".flv"] = [new(MagicBytesCheckStrategy.StartsWith, [0x46, 0x4c, 0x56])], // FLV

        // binaries
        [".elf"] = [new(MagicBytesCheckStrategy.StartsWith, [0x7f, 0x45, 0x4c, 0x46])], // ELF
        [".exe"] = [new(MagicBytesCheckStrategy.StartsWith, [0x4d, 0x5a])], // MZ
        [".wasm"] = [new(MagicBytesCheckStrategy.StartsWith, [0x00, 0x61, 0x73, 0x6d])],

        // fonts
        [".woff"] = [new(MagicBytesCheckStrategy.StartsWith, [0x77, 0x4f, 0x46, 0x46])],

        [".woff2"] = [new(MagicBytesCheckStrategy.StartsWith, [0x77, 0x4f, 0x46, 0x32])],

        // misc
        [".db"] = [
            new(MagicBytesCheckStrategy.StartsWith, [
                0x53,0x51,0x4c,0x69,0x74,0x65,0x20,0x66,
                0x6f,0x72,0x6d,0x61,0x74,0x20,0x33,0x00,
            ]),
        ],
        [".psd"] = [new(MagicBytesCheckStrategy.StartsWith, [0x38, 0x42, 0x50, 0x53])], // 
        [".swf"] = [
            new(MagicBytesCheckStrategy.StartsWithAnyOf, [], Candidates: [
                [0x43,0x57,0x53], // CWS compressed
                [0x46,0x57,0x53], // FWS uncompressed
            ]),
        ]
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    #endregion

    /// <summary>
    /// Looks up signatures for a given extension, resolving aliases to canonical forms.
    /// </summary>
    private static MagicBytesSignature[]? TryGetSignatures(string fileExtension) {
        // First check if this is an alias and resolve to canonical form
        var canonicalExtension = ExtensionAliases.GetValueOrDefault(fileExtension, fileExtension);
        // Then lookup the signatures
        return ByFileExtension.GetValueOrDefault(canonicalExtension);
    }
    /// <summary>
    /// Dispatches a single signature check to the appropriate strategy implementation.
    /// </summary>
    private static bool Evaluate(ReadOnlySpan<byte> buf, MagicBytesSignature sig) => sig.Strategy switch {
        MagicBytesCheckStrategy.StartsWith => StartsWithCheck(buf, sig.Bytes),
        MagicBytesCheckStrategy.EndsWith => EndsWithCheck(buf, sig.Bytes),
        MagicBytesCheckStrategy.Anywhere => AnywhereCheck(buf, sig.Bytes),
        MagicBytesCheckStrategy.AnywhereAnyOf => AnywhereAnyOfCheck(buf, sig.Candidates ?? throw new ArgumentNullException(nameof(sig.Candidates))),
        MagicBytesCheckStrategy.StartsWithAnyOf => StartsWithAnyOfCheck(buf, sig.Candidates ?? throw new ArgumentNullException(nameof(sig.Candidates))),
        MagicBytesCheckStrategy.EndsWithAnyOf => EndsWithAnyOfCheck(buf, sig.Candidates ?? throw new ArgumentNullException(nameof(sig.Candidates))),
        MagicBytesCheckStrategy.Offset => OffsetCheck(buf, sig.Bytes, sig.Offset),
        _ => throw new ArgumentOutOfRangeException(nameof(sig.Strategy), sig.Strategy, null),
    };

    private static (int offset, int count) GetRequiredBytesRange(MagicBytesSignature sig, long streamLength) {
        return sig.Strategy switch {
            MagicBytesCheckStrategy.StartsWith => (0, sig.Bytes.Length),
            MagicBytesCheckStrategy.EndsWith => ((int)(streamLength - sig.Bytes.Length), sig.Bytes.Length),
            MagicBytesCheckStrategy.Anywhere => (0, (int)streamLength),
            MagicBytesCheckStrategy.AnywhereAnyOf => (0, (int)streamLength),
            MagicBytesCheckStrategy.StartsWithAnyOf => (0, sig.Candidates!.Max(c => c.Length)),
            MagicBytesCheckStrategy.EndsWithAnyOf => ((int)(streamLength - sig.Candidates!.Max(c => c.Length)), sig.Candidates!.Max(c => c.Length)),
            MagicBytesCheckStrategy.Offset => (0, sig.Offset + sig.Bytes.Length),
            _ => throw new ArgumentOutOfRangeException(nameof(sig.Strategy), sig.Strategy, null)
        };
    }

    private static bool StartsWithCheck(ReadOnlySpan<byte> buf, byte[] bytes) =>
        buf.Length >= bytes.Length && buf[..bytes.Length].SequenceEqual(bytes);

    private static bool EndsWithCheck(ReadOnlySpan<byte> buf, byte[] bytes) =>
        buf.Length >= bytes.Length && buf[^bytes.Length..].SequenceEqual(bytes);

    private static bool EndsWithAnyOfCheck(ReadOnlySpan<byte> buf, byte[][] candidates) {
        foreach (var cand in candidates)
            if (buf.Length >= cand.Length && buf[^cand.Length..].SequenceEqual(cand))
                return true;
        return false;
    }

    private static bool AnywhereCheck(ReadOnlySpan<byte> buf, byte[] needle) {
        if (buf.Length < needle.Length) return false;
        int limit = buf.Length - needle.Length;
        for (int i = 0; i <= limit; i++)
            if (buf.Slice(i, needle.Length).SequenceEqual(needle))
                return true;
        return false;
    }

    private static bool AnywhereAnyOfCheck(ReadOnlySpan<byte> buf, byte[][] candidates) {
        foreach (var cand in candidates) {
            if (buf.Length < cand.Length) continue;
            int limit = buf.Length - cand.Length;
            for (int i = 0; i <= limit; i++)
                if (buf.Slice(i, cand.Length).SequenceEqual(cand))
                    return true;
        }
        return false;
    }

    private static bool StartsWithAnyOfCheck(ReadOnlySpan<byte> buf, byte[][] candidates) {
        foreach (var cand in candidates)
            if (buf.Length >= cand.Length && buf[..cand.Length].SequenceEqual(cand))
                return true;
        return false;
    }

    private static bool OffsetCheck(ReadOnlySpan<byte> buf, byte[] bytes, int offset) =>
        buf.Length >= offset + bytes.Length &&
        buf.Slice(offset, bytes.Length).SequenceEqual(bytes);


    /// <inheritdoc/>
    public async Task<MagicBytesValidationResult> IsValid(Stream fileStream, string fileExtension, CancellationToken ct) {
        if (string.IsNullOrWhiteSpace(fileExtension)) {
            return MagicBytesValidationResult.Valid();
        }

        if (!fileExtension.StartsWith('.')) {
            fileExtension = '.' + fileExtension;
        }

        var signatures = TryGetSignatures(fileExtension);
        if (signatures is null || signatures.Length == 0) {
            return MagicBytesValidationResult.UnknownExtension($"No signatures registered for extension '{fileExtension}'.");
        }

        foreach (var sig in signatures) {
            var (offset, count) = GetRequiredBytesRange(sig, fileStream.Length);

            if (offset < 0 || offset + count > fileStream.Length) {
                return MagicBytesValidationResult.Failure($"Magic bytes check failed for extension '{fileExtension}'.");
            }

            var buffer = ArrayPool<byte>.Shared.Rent(count);

            try {
                fileStream.Seek(offset, SeekOrigin.Begin);
                var read = await fileStream.ReadAtLeastAsync(buffer.AsMemory(0, count), count, throwOnEndOfStream: false, ct);

                if (!Evaluate(buffer.AsSpan(0, read), sig)) {
                    return MagicBytesValidationResult.Failure($"Magic bytes check failed for extension '{fileExtension}'.");
                }
            } finally {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        return MagicBytesValidationResult.Valid();
    }
}