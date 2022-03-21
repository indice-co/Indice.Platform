using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Indice.AspNetCore.Features.Campaigns.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Indice.AspNetCore.Features.Campaigns
{
    internal class CampaignEndpointFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        private static IReadOnlyList<TypeInfo> ManagementApiControllerTypes => new List<TypeInfo>() {
            typeof(CampaignsController).GetTypeInfo(),
            typeof(MessageTypesController).GetTypeInfo()
        };

        private static IReadOnlyList<TypeInfo> UserApiControllerTypes => new List<TypeInfo>() {
            typeof(InboxController).GetTypeInfo()
        };

        public CampaignEndpointFeatureProvider(bool includeManagementApi = true, bool includeUserApi = true) {
            IncludeManagementApi = includeManagementApi;
            IncludeUserApi = includeUserApi;
        }

        public bool IncludeManagementApi { get; }
        public bool IncludeUserApi { get; }

        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature) {
            if (IncludeManagementApi) {
                AddControllers(feature, ManagementApiControllerTypes);
            }
            if (IncludeUserApi) {
                AddControllers(feature, UserApiControllerTypes);
            }
        }

        private void AddControllers(ControllerFeature feature, IReadOnlyList<TypeInfo> controllerTypes) {
            foreach (var type in controllerTypes) {
                if (!feature.Controllers.Any(x => x.FullName == type.FullName)) {
                    feature.Controllers.Add(type);
                }
            }
        }
    }
}