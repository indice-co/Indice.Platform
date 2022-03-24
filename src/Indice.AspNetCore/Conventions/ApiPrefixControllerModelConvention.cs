using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Indice.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// Allows to dynamically set the route prefix of a controller.
    /// </summary>
    /// <remarks>https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/application-model?view=aspnetcore-5.0#modify-the-controllermodel-description</remarks>
    public class ApiPrefixControllerModelConvention : IControllerModelConvention
    {
        private readonly string _templatePrefixPlaceholder;
        private readonly string _replacementValue;

        /// <summary>
        /// Creates a new instance of <see cref="ApiPrefixControllerModelConvention"/>.
        /// </summary>
        /// <param name="templatePrefixPlaceholder">The placeholder used in the controller route of the <see cref="RouteAttribute"/>.</param>
        /// <param name="replacementValue">The value to replace.</param>
        public ApiPrefixControllerModelConvention(string templatePrefixPlaceholder, string replacementValue) {
            _templatePrefixPlaceholder = templatePrefixPlaceholder ?? throw new ArgumentNullException(nameof(templatePrefixPlaceholder));
            _replacementValue = replacementValue;
        }

        /// <inheritdoc />
        public void Apply(ControllerModel controller) {
            var selector = controller.Selectors[0];
            if (selector.AttributeRouteModel == null) {
                selector.AttributeRouteModel = new AttributeRouteModel();
            }
            if (!string.IsNullOrWhiteSpace(_replacementValue)) {
                selector.AttributeRouteModel.Template = selector.AttributeRouteModel.Template?.Replace(_templatePrefixPlaceholder, _replacementValue);
            } else {
                selector.AttributeRouteModel.Template = selector.AttributeRouteModel.Template?.Replace($"{_templatePrefixPlaceholder}/", string.Empty);
            }
        }
    }
}
