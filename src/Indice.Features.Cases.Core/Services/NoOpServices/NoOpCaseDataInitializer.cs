using System.Text.Json.Nodes;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;

namespace Indice.Features.Cases.Core.Services.NoOpServices;

internal class NoOpCaseDataInitializer : ICaseDataInitializer
{
    public Task<JsonNode?> InitializeAsync(Contact? contact, string caseTypeCode) =>
        Task.FromResult(
            caseTypeCode is "SampleAddress"
            ? JsonNode.Parse("""
                {
                    "postOfficeBox" : "123",
                    "streetAddress" : "456 Main St",
                    "locality" : "Cityville",
                    "region" : "Attica",
                    "postalCode" : "12345",
                    "countryName": "Greece"
                }
                """)
            : JsonNode.Parse("{}")
        );
}