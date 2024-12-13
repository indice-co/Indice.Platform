namespace Indice.Features.GovGr.Models;

/// <summary>The response from Bancapp GCloud Upload File endpoint.</summary>
public class BancappGCloudUploadResponse
{
    /// <summary>The status of the response.</summary>
    public bool Succeeded { get; private set;}

    /// <summary>Error message.</summary>
    public string? ErrorMessage { get; set;}

    /// <summary>Set result status to Success.</summary>
    internal static BancappGCloudUploadResponse Success() => new BancappGCloudUploadResponse {Succeeded = true};

    internal static BancappGCloudUploadResponse Fail(string error) {
        return new BancappGCloudUploadResponse {
            Succeeded = false,
            ErrorMessage = $"Response file upload to G-Cloud failed. {error}"
        };
    }

    /// <summary>Creates <see cref="BancappGCloudUploadResponse"/> from <see cref="HttpResponseMessage"/></summary>
    /// <exception cref="ArgumentNullException"></exception>
    internal static async Task<BancappGCloudUploadResponse> FromHttpResponseMessage(HttpResponseMessage httpResponseMessage) {
        if (httpResponseMessage is null) {
            throw new ArgumentNullException(nameof(httpResponseMessage));
        }
        
        if (httpResponseMessage.IsSuccessStatusCode) {
            return Success();
        }
        
        // Failed G-Cloud requests return only a string validation error in case of StatusCode: 400
        return Fail($"Status Code: {(int)httpResponseMessage.StatusCode}, " +
             $"Reason: {httpResponseMessage.ReasonPhrase} {await httpResponseMessage.Content.ReadAsStringAsync()}");
    }
}