using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Indice.Features.Identity.UI;

internal class IdentityUIPageModelConvention : IPageApplicationModelConvention
{
    public void Apply(PageApplicationModel model) {
        var identityUIAttribute = model.ModelType?.GetCustomAttribute<IdentityUIAttribute>();
        if (identityUIAttribute is null) {
            return;
        }
        model.ModelType = identityUIAttribute.Template.GetTypeInfo();
    }
}
