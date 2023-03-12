using Azure;
using Azure.Storage.Blobs.Models;

namespace Indice.Extensions;

/// <summary>Extensions of type <see cref="BlobProperties"/></summary>
public static class BlobPropertiesExtensions
{
    /// <summary>
    /// Wraps eTag with quotes if not already wrapped.
    /// This a workaround for https://github.com/Azure/azure-sdk-for-net/issues/22877 
    /// </summary>
    /// <param name="value">Etag to wrap with quotes if needed</param>
    /// <returns>Etag string wrapped with quotes</returns>
    public static string GetHttpSafeETag(this ETag value) {
        //TODO Remove when https://github.com/Azure/azure-sdk-for-net/issues/22877 is fixed
        var eTag = value.ToString();
        if (!string.IsNullOrEmpty(eTag)) {
            if (!(eTag.StartsWith("\"") || eTag.StartsWith("W"))) {
                eTag = string.Format("\"{0}\"", eTag);
            }
        }
        return eTag;
    }
}
