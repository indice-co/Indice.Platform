using System;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Indice.AspNetCore.Features.Campaigns.Mvc.ApplicationModels
{
    // https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/application-model?view=aspnetcore-5.0#modify-the-controllermodel-description
    internal class ApiPrefixControllerModelConvention : IControllerModelConvention
    {
        public ApiPrefixControllerModelConvention(CampaignsApiOptions campaignsApiOptions) {
            CampaignsApiOptions = campaignsApiOptions ?? throw new ArgumentNullException(nameof(campaignsApiOptions));
        }

        public CampaignsApiOptions CampaignsApiOptions { get; }

        public void Apply(ControllerModel controller) {
            var selector = controller.Selectors[0];
            if (selector.AttributeRouteModel == null) { 
                selector.AttributeRouteModel = new AttributeRouteModel();
            }
            selector.AttributeRouteModel.Template = selector.AttributeRouteModel.Template?.Replace("[campaignsApiPrefix]", CampaignsApiOptions.ApiPrefix ?? "api");
        }
    }
}
