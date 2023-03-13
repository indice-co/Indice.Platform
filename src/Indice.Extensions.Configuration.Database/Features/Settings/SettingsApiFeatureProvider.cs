using System.Reflection;
using Indice.AspNetCore.Features.Settings.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Indice.AspNetCore.Features.Settings;

internal class SettingsApiFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    private static IReadOnlyList<TypeInfo> ControllerTypes => new List<TypeInfo>() {
        typeof(SettingsController).GetTypeInfo()
    };

    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature) {
        foreach (var type in ControllerTypes) {
            if (!feature.Controllers.Any(x => x.FullName == type.FullName)) {
                feature.Controllers.Add(type);
            }
        }
    }
}
