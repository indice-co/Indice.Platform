using Microsoft.AspNetCore.Mvc.Controllers;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Indice.AspNetCore.Swagger
{

    internal class SimpleOperationIdFilter : IOperationFilter
    {
        /// <summary>
        /// Changes the operation id to the action method name.
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="context"></param>
        public void Apply(Operation operation, OperationFilterContext context) {
            var actionDescriptor = context.ApiDescription.ActionDescriptor as ControllerActionDescriptor;
            operation.OperationId = actionDescriptor.ActionName;
        }
    }
}
