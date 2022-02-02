using System;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.AspNetCore.Swagger;
using Indice.Types;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Indice.Api.Swagger
{
    public class SchemaExamplesFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context) {
            if (context.Type == typeof(UpsertCampaignTypeRequest)) {
                schema.Example = new UpsertCampaignTypeRequest {
                    Name = "Informational"
                }
                .ToOpenApiAny();
            }
            if (context.Type == typeof(CreateCampaignRequest)) {
                schema.Example = new CreateCampaignRequest {
                    ActionText = "Visit Google now",
                    Title = "Google it",
                    Content = "Visit Google's search engine and find everything you need instantly!",
                    ActionUrl = "https://www.google.com",
                    ActivePeriod = new Period {
                        From = DateTimeOffset.UtcNow
                    },
                    DeliveryChannel = CampaignDeliveryChannel.Inbox | CampaignDeliveryChannel.PushNotification,
                    IsGlobal = true,
                    Published = true
                }
                .ToOpenApiAny();
            }
        }
    }
}
