using System.Text;

namespace System.IO.Compression;

/// <summary>Utility methods for compression and decompression.</summary>
/// <remarks>https://khalidabuhakmeh.com/compress-strings-with-dotnet-and-csharp</remarks>
public class CompressionUtils
{
    /// <summary>Compresses the given payload.</summary>
    /// <param name="payload">The payload to compress.</param>
    /// <returns>The payload compressed as a byte array.</returns>
    public static async Task<byte[]> CompressFromString(string payload) => await Compress(Encoding.Unicode.GetBytes(payload));

    /// <summary>Decompresses the given byte array.</summary>
    /// <param name="compressedBytes">The compressed bytes.</param>
    /// <returns>The payload decompressed as a byte array.</returns>
    public static async Task<string> DecompressToString(byte[] compressedBytes) => Encoding.Unicode.GetString(await Decompress(compressedBytes));

    /// <summary>Compresses the given payload.</summary>
    /// <param name="payload">The payload to compress.</param>
    /// <returns>The payload compressed as a byte array.</returns>
    public static async Task<byte[]> Compress(byte[] payload) {
        await using var input = new MemoryStream(payload);
        await using var output = new MemoryStream();
        await using var stream = new BrotliStream(output, CompressionLevel.Optimal);
        await input.CopyToAsync(stream);
        await stream.FlushAsync();
        return output.ToArray();
    }

    /// <summary>Decompresses the given byte array.</summary>
    /// <param name="compressedBytes">The compressed bytes.</param>
    /// <returns>The payload decompressed as a byte array.</returns>
    public static async Task<byte[]> Decompress(byte[] compressedBytes) {
        await using var input = new MemoryStream(compressedBytes);
        await using var output = new MemoryStream();
        await using var stream = new BrotliStream(input, CompressionMode.Decompress);
        await stream.CopyToAsync(output);
        return output.ToArray();
    }
}
