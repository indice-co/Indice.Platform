using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Indice.AspNetCore.Features.Campaigns.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Indice.AspNetCore.Features.Campaigns
{
    internal class CampaignsApiFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        private static IReadOnlyList<TypeInfo> ControllerTypes => new List<TypeInfo>() {
            typeof(CampaignsController).GetTypeInfo(),
            typeof(MyMessagesController).GetTypeInfo()
        };

        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature) {
            foreach (var type in ControllerTypes) {
                if (!feature.Controllers.Any(x => x.FullName == type.FullName)) {
                    feature.Controllers.Add(type);
                }
            }
        }
    }
}