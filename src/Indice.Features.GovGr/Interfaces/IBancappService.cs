using Indice.Features.GovGr.Models;

namespace Indice.Features.GovGr.Interfaces;

/// <summary>The bancapp API integration service. Use to communicate with Gov GCloud.</summary>
public interface IBancappService
{
    /// <summary>Upload file to Gov GCloud.</summary>
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