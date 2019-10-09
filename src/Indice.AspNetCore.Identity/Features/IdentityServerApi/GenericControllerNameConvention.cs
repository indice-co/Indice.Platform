using System;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Fixes name of a controller that has generic type parameters.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    internal class GenericControllerNameConvention : Attribute, IControllerModelConvention
    {
        public void Apply(ControllerModel controller) {
            if (!controller.ControllerType.IsGenericType) {
                return;
            }
            controller.ControllerName = controller.ControllerName
                                                  .Substring(0, controller.ControllerName.IndexOf("`"))
                                                  .Replace("Controller", string.Empty);
        }
    }
}
