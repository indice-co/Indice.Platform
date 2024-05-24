using Indice.Features.GovGr.Models;

namespace Indice.Features.GovGr.Interfaces;

/// <summary>The bancapp API integration service. Use to communicate with Gov GCloud</summary>
public interface IBancappService
{
    /// <summary>Upload file to Gov GCloud.</summary>
    Task<BancappGCloudUploadResponse> UploadFile(byte[] fileBytes, string fileName);
}
