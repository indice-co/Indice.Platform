using System.Collections.Frozen;

namespace Indice.Services;

/// <summary>Service that validates uploaded files by reading their magic bytes (file signatures).</summary>
public interface IMagicBytesValidator
{
    /// <summary>Validates that a stream's content matches the expected magic bytes for the given file extension.</summary>
    /// <param name="bytes">The file bytes to validate.</param>
    /// <param name="fileExtension">The file extension (e.g. ".jpg", ".png").</param>
    /// <returns>
    /// <see langword="true"/> if the file content matches the expected magic bytes for the extension,
    /// or if the extension does not have a known signature; otherwise <see langword="false"/>.
    /// </returns>
    MagicBytesValidationResult IsValid(byte[] bytes, string fileExtension);

    /// <summary>Validates that a stream's content matches the expected magic bytes for the given file extension.</summary>
    /// <param name="bytes">The file bytes as Span to validate.</param>
    /// <param name="fileExtension">The file extension (e.g. ".jpg", ".png").</param>
    /// <returns>
    /// <see langword="true"/> if the file content matches the expected magic bytes for the extension,
    /// or if the extension does not have a known signature; otherwise <see langword="false"/>.
    /// </returns>
    MagicBytesValidationResult IsValid(ReadOnlySpan<byte> bytes, string fileExtension);
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
/// Maps a file extension to one or more magic byte signatures that identify it.
/// </summary>
/// <param name="FileExtensions">The file extension(s) this entry represents (e.g. ".docx/.xlsx/.pptx").</param>
/// <param name="Signatures">All signatures that must collectively identify the file type.</param>
public sealed record MagicBytesSignatureEntry(string FileExtensions, MagicBytesSignature[] Signatures);

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
    private static FrozenDictionary<string, MagicBytesSignature[]>? ByFileExtension;

    #region Magic Bytes table
    private static readonly MagicBytesSignatureEntry[] Entries =
    [
        // images
        new(".png", [
            new(MagicBytesCheckStrategy.StartsWith, [0x89,0x50,0x4e,0x47,0x0d,0x0a,0x1a,0x0a]),
        ]),
        new(".svg", [
            new(MagicBytesCheckStrategy.Anywhere,      [0x3C, 0x73, 0x76, 0x67]),
            new(MagicBytesCheckStrategy.EndsWithAnyOf, [], Candidates: [
                [0x3C, 0x2F, 0x73, 0x76, 0x67, 0x3E],
                [0x3C, 0x2F, 0x73, 0x76, 0x67, 0x3E, 0x0A],
                [0x3C, 0x2F, 0x73, 0x76, 0x67, 0x3E, 0x0D, 0x0A],
            ]),
        ]),
        new(".jpeg/.jpg", [
            new(MagicBytesCheckStrategy.StartsWithAnyOf, [], Candidates: [
                [0xff,0xd8,0xff,0xe0],
                [0xff,0xd8,0xff,0xe1],
                [0xff,0xd8,0xff,0xee],
                [0xff,0xd8,0xff,0xdb],
            ]),
        ]),
        new(".gif", [
            new(MagicBytesCheckStrategy.StartsWithAnyOf, [], Candidates: [
                [0x47,0x49,0x46,0x38,0x37,0x61],
                [0x47,0x49,0x46,0x38,0x39,0x61],
            ]),
        ]),
        new(".webp", [
            new(MagicBytesCheckStrategy.StartsWith, [0x52,0x49,0x46,0x46]),
            new(MagicBytesCheckStrategy.Offset,     [0x57,0x45,0x42,0x50], Offset: 8),
        ]),
        new(".bmp", [
            new(MagicBytesCheckStrategy.StartsWith, [0x42,0x4d]),
        ]),
        new(".tif/.tiff", [
            new(MagicBytesCheckStrategy.StartsWithAnyOf, [], Candidates: [
                [0x49,0x49,0x2a,0x00],
                [0x4d,0x4d,0x00,0x2a],
            ]),
        ]),
        new(".avif", [
            new(MagicBytesCheckStrategy.Offset, [0x66,0x74,0x79,0x70,0x61,0x76,0x69,0x66], Offset: 4),
        ]),
        new(".heic/heif", [
            new(MagicBytesCheckStrategy.StartsWithAnyOf, [], Candidates: [
                [0x66,0x74,0x79,0x70,0x68,0x65,0x69,0x63],
                [0x66,0x74,0x79,0x70,0x68,0x65,0x69,0x78],
            ]),
        ]),
        new(".ico", [
            new(MagicBytesCheckStrategy.StartsWith, [0x00,0x00,0x01,0x00]),
        ]),

        // documents
        new(".pdf", [
            new(MagicBytesCheckStrategy.StartsWith, [0x25,0x50,0x44,0x46]), // %PDF
        ]),
        new(".doc", [
            new(MagicBytesCheckStrategy.StartsWith, [0xd0,0xcf,0x11,0xe0,0xa1,0xb1,0x1a,0xe1]),
        ]),
        new(".docx/.xlsx/.pptx", [ // OOXML (docx/xlsx/pptx) are ZIP-based
            new(MagicBytesCheckStrategy.StartsWith, [0x50,0x4b,0x03,0x04]),
        ]),
        new(".ps", [
            new(MagicBytesCheckStrategy.StartsWith, [0x25,0x21,0x50,0x53]), // %!PS
        ]),

        // archives
        new(".zip", [
            new(MagicBytesCheckStrategy.StartsWithAnyOf, [], Candidates: [
                [0x50,0x4b,0x03,0x04],
                [0x50,0x4b,0x05,0x06], // empty
                [0x50,0x4b,0x07,0x08], // spanned
            ]),
        ]),
        new(".rar", [
            new(MagicBytesCheckStrategy.StartsWithAnyOf, [], Candidates: [
                [0x52,0x61,0x72,0x21,0x1a,0x07,0x00],      // RAR4
                [0x52,0x61,0x72,0x21,0x1a,0x07,0x01,0x00], // RAR5
            ]),
        ]),
        new(".7z", [
            new(MagicBytesCheckStrategy.StartsWith, [0x37,0x7a,0xbc,0xaf,0x27,0x1c]),
        ]),
        new(".gz", [
            new(MagicBytesCheckStrategy.StartsWith, [0x1f,0x8b]),
        ]),
        new(".bz2", [
            new(MagicBytesCheckStrategy.StartsWith, [0x42,0x5a,0x68]), // BZh
        ]),
        new(".xz", [
            new(MagicBytesCheckStrategy.StartsWith, [0xfd,0x37,0x7a,0x58,0x5a,0x00]),
        ]),
        new(".zst", [
            new(MagicBytesCheckStrategy.StartsWith, [0x28,0xb5,0x2f,0xfd]),
        ]),
        new(".tar", [
            new(MagicBytesCheckStrategy.StartsWithAnyOf, [], Candidates: [
                [0x75,0x73,0x74,0x61,0x72,0x00,0x30,0x30], // ustar\000
                [0x75,0x73,0x74,0x61,0x72,0x20,0x20,0x00], // ustar  \0
            ]),
        ]),

        // audio
        new(".mp3", [
            new(MagicBytesCheckStrategy.StartsWithAnyOf, [], Candidates: [
                [0xff,0xfb],
                [0xff,0xf3],
                [0xff,0xf2],
                [0x49,0x44,0x33], // ID3
            ]),
        ]),
        new(".ogg", [
            new(MagicBytesCheckStrategy.StartsWith, [0x4f,0x67,0x67,0x53]), // OggS
        ]),
        new(".flac", [
            new(MagicBytesCheckStrategy.StartsWith, [0x66,0x4c,0x61,0x43]), // fLaC
        ]),
        new(".wav", [
            new(MagicBytesCheckStrategy.StartsWith, [0x52,0x49,0x46,0x46]),
            new(MagicBytesCheckStrategy.Offset,     [0x57,0x41,0x56,0x45], Offset: 8),
        ]),
        new(".mid", [
            new(MagicBytesCheckStrategy.StartsWith, [0x4d,0x54,0x68,0x64]), // MThd
        ]),
        new(".aac", [
            new(MagicBytesCheckStrategy.StartsWithAnyOf, [], Candidates: [
                [0xff,0xf1],
                [0xff,0xf9],
            ]),
        ]),

        // video
        new(".mp4", [
            new(MagicBytesCheckStrategy.AnywhereAnyOf, [], Candidates: [
                [0x66,0x74,0x79,0x70,0x69,0x73,0x6f,0x6d], // ftypisom
                [0x66,0x74,0x79,0x70,0x6d,0x70,0x34,0x32], // ftypmp42
                [0x66,0x74,0x79,0x70,0x4d,0x53,0x4e,0x56], // ftypMSNV
            ]),
        ]),
        new(".webm", [
            new(MagicBytesCheckStrategy.StartsWith, [0x1a,0x45,0xdf,0xa3]),
        ]),
        new(".mkv", [
            new(MagicBytesCheckStrategy.StartsWith, [0x1a,0x45,0xdf,0xa3]),
        ]),
        new(".avi", [
            new(MagicBytesCheckStrategy.StartsWith, [0x52,0x49,0x46,0x46]),
            new(MagicBytesCheckStrategy.Offset,     [0x41,0x56,0x49,0x20], Offset: 8),
        ]),
        new(".wmv", [
            new(MagicBytesCheckStrategy.StartsWith, [0x30,0x26,0xb2,0x75,0x8e,0x66,0xcf,0x11]),
        ]),
        new(".3gp", [
            new(MagicBytesCheckStrategy.Offset, [0x66,0x74,0x79,0x70,0x33,0x67], Offset: 4),
        ]),
        new(".flv", [
            new(MagicBytesCheckStrategy.StartsWith, [0x46,0x4c,0x56]), // FLV
        ]),

        // binaries
        new(".elf", [
            new(MagicBytesCheckStrategy.StartsWith, [0x7f,0x45,0x4c,0x46]), // ELF
        ]),
        new(".exe", [
            new(MagicBytesCheckStrategy.StartsWith, [0x4d,0x5a]), // MZ
        ]),
        new(".wasm", [
            new(MagicBytesCheckStrategy.StartsWith, [0x00,0x61,0x73,0x6d]),
        ]),

        // fonts
        new(".woff", [
            new(MagicBytesCheckStrategy.StartsWith, [0x77,0x4f,0x46,0x46]),
        ]),
        new(".woff2", [
            new(MagicBytesCheckStrategy.StartsWith, [0x77,0x4f,0x46,0x32]),
        ]),

        // misc
        new(".db", [
            new(MagicBytesCheckStrategy.StartsWith, [
                0x53,0x51,0x4c,0x69,0x74,0x65,0x20,0x66,
                0x6f,0x72,0x6d,0x61,0x74,0x20,0x33,0x00,
            ]),
        ]),
        new(".psd", [
            new(MagicBytesCheckStrategy.StartsWith, [0x38,0x42,0x50,0x53]), // 8BPS
        ]),
        new(".swf", [
            new(MagicBytesCheckStrategy.StartsWithAnyOf, [], Candidates: [
                [0x43,0x57,0x53], // CWS compressed
                [0x46,0x57,0x53], // FWS uncompressed
            ]),
        ])
    ];
    #endregion

    /// <summary>
    /// Looks up signatures for a given extension, supporting slash-delimited multi-extension keys.
    /// </summary>
    private static MagicBytesSignature[]? TryGetSignatures(string fileExtension) =>
        ByFileExtension!
             .FirstOrDefault(kvp =>
                 kvp.Key.Split('/', StringSplitOptions.RemoveEmptyEntries)
                    .Any(ext => string.Equals(ext.Trim(), fileExtension, StringComparison.OrdinalIgnoreCase))
             ).Value ?? null;

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

    /// <summary>
    /// Core validation logic. Returns <c>true</c> if all signatures for the given extension match,
    /// or if the extension is unknown (no signatures registered). Returns <c>false</c> if the
    /// extension is known but any signature check fails.
    /// </summary>
    internal MagicBytesValidationResult ValidateInternal(ReadOnlySpan<byte> bytes, string fileExtension) {
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
            if (!Evaluate(bytes, sig)) {
                return MagicBytesValidationResult.Failure($"Magic bytes check failed for extension '{fileExtension}'.");
            }
        }

        return MagicBytesValidationResult.Valid();
    }

    /// <summary>
    /// Builds the frozen lookup dictionary from <see cref="Entries"/> on first instantiation.
    /// </summary>
    public MagicBytesValidator() {
        ByFileExtension = Entries.ToFrozenDictionary(e => e.FileExtensions, e => e.Signatures, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public MagicBytesValidationResult IsValid(byte[] bytes, string fileExtension) {
        return ValidateInternal(bytes.AsSpan(), fileExtension);
    }

    /// <inheritdoc/>
    public MagicBytesValidationResult IsValid(ReadOnlySpan<byte> bytes, string fileExtension) {
        return ValidateInternal(bytes, fileExtension);
    }
}