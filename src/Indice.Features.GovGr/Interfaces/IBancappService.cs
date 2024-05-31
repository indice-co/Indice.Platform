using Indice.Features.GovGr.Models;

namespace Indice.Features.GovGr.Interfaces;

/// <summary>The bancapp API integration service. Use to communicate with Gov GCloud.</summary>
public interface IBancappService
{
    /// <summary>Upload file to Gov GCloud.</summary>
    /// <param name="stream"><br/>The <see cref="Stream"/> needs to be open/seekable. <br/> The service will always read the stream from the beginning regardless of its position. <br/> The caller is responsible for disposing the <see cref="Stream"/> object that is passed.</param>
    /// <param name="fileName"></param>
    /// <returns></returns>
    Task<BancappGCloudUploadResponse> UploadFile(Stream stream, string fileName);
}

/// <summary>BancappServiceExtensions</summary>
public static class BancappServiceExtensions
{
    /// <summary></summary>
    public static async Task<BancappGCloudUploadResponse> UploadFile(this IBancappService service, byte[] byteArray, string fileName) {
        using var memoryStream = new MemoryStream(byteArray);
        return await service.UploadFile(memoryStream, fileName);
    }
}