using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Indice.Features.Cases.Mvc.Conventions
{
    // https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/application-model?view=aspnetcore-5.0#modify-the-controllermodel-description
    internal class ApiPrefixControllerModelConvention : IControllerModelConvention
    {
        private readonly CasesApiOptions _casesApiOptions;

        public ApiPrefixControllerModelConvention(CasesApiOptions casesApiOptions) {
            _casesApiOptions = casesApiOptions ?? throw new ArgumentNullException(nameof(casesApiOptions));
        }

        public void Apply(ControllerModel controller) {
            var selector = controller.Selectors[0];
            selector.AttributeRouteModel ??= new AttributeRouteModel();
            selector.AttributeRouteModel.Template = selector.AttributeRouteModel.Template?.Replace("[casesApiPrefix]", _casesApiOptions.ApiPrefix ?? "api");
        }
    }
}
