using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using IdentityServer4.ResponseHandling;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.Identity.Features
{
    internal class AuthorizationErrorResult : IEndpointResult
    {
        private readonly TokenErrorResponse _tokenErrorResponse;

        public AuthorizationErrorResult(TokenErrorResponse tokenErrorResponse) {
            if (string.IsNullOrWhiteSpace(tokenErrorResponse.Error)) {
                throw new ArgumentNullException(nameof(tokenErrorResponse.Error), "Error must be set.");
            }
            _tokenErrorResponse = tokenErrorResponse;
        }

        public async Task ExecuteAsync(HttpContext context) {
            context.Response.StatusCode = 400;
            context.Response.SetNoCache();
            var result = new ErrorResultDto {
                Error = _tokenErrorResponse.Error,
                ErrorDescription = _tokenErrorResponse.ErrorDescription,
                Custom = _tokenErrorResponse.Custom
            };
            await context.Response.WriteJsonAsync(result);
        }
    }

    internal class ErrorResultDto
    {
        [JsonPropertyName("error")]
        public string Error { get; set; }
        [JsonPropertyName("error_description")]
        public string ErrorDescription { get; set; }
        [JsonExtensionData]
        [JsonPropertyName("custom")]
        public Dictionary<string, object> Custom { get; set; }
    }
}
