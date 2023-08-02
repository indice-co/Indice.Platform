using System.Reflection;
using Indice.Features.Messages.AspNetCore.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Indice.Features.Messages.AspNetCore;

internal class MessageFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    private static IReadOnlyList<TypeInfo> ManagementApiControllerTypes => new List<TypeInfo>() {
        typeof(CampaignsController).GetTypeInfo(),
        typeof(ContactsController).GetTypeInfo(),
        typeof(DistributionListsController).GetTypeInfo(),
        typeof(MessageTypesController).GetTypeInfo(),
        typeof(MessageSendersController).GetTypeInfo(),
        typeof(TemplatesController).GetTypeInfo()
    };

    private static IReadOnlyList<TypeInfo> InboxApiControllerTypes => new List<TypeInfo>() {
        typeof(InboxController).GetTypeInfo(),
        typeof(TrackingController).GetTypeInfo()
    };

    public MessageFeatureProvider(bool includeManagementApi = true, bool includeInboxApi = true) {
        IncludeManagementApi = includeManagementApi;
        IncludeInboxApi = includeInboxApi;
    }

    public bool IncludeManagementApi { get; }
    public bool IncludeInboxApi { get; }

    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature) {
        if (IncludeManagementApi) {
            AddControllers(feature, ManagementApiControllerTypes);
        }
        if (IncludeInboxApi) {
            AddControllers(feature, InboxApiControllerTypes);
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
