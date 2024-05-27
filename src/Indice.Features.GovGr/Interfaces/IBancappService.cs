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
    /// <summary>Covert <see cref="byte"/> <see cref="Array"/> to <see cref="Stream"/>.</summary>
    public static Stream ConvertToMemoryStream(byte[] byteArray) {
        return new MemoryStream(byteArray);
    }
}