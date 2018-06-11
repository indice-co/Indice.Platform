using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Dynamic;

namespace Indice.AspNetCore.Swagger
{
    internal class FormFileOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
                return;

            var formFileParams = context.ApiDescription.ActionDescriptor.Parameters
                                    .Where(x => x.ParameterType.IsAssignableFrom(typeof(IFormFile)))
                                    .Select(x => x.Name)
                                    .ToList();

            var formFileSubParams = context.ApiDescription.ActionDescriptor.Parameters
                .SelectMany(x => x.ParameterType.GetProperties())
                .Where(x => x.PropertyType != typeof(object) && x.PropertyType.IsAssignableFrom(typeof(IFormFile)))
                .Select(x => x.Name)
                .ToList();

            var allFileParamNames = formFileParams.Union(formFileSubParams);

            if (!allFileParamNames.Any())
                return;

            var paramsToRemove = new List<IParameter>();
            var formFileNames = typeof(IFormFile).GetProperties().Select(x => x.Name).ToArray();
            paramsToRemove.AddRange(from param in operation.Parameters where param.In == "formData" && formFileNames.Contains(param.Name) select param);
            paramsToRemove.ForEach(x => operation.Parameters.Remove(x));
            foreach (var paramName in allFileParamNames)
            {
                var fileParam = new NonBodyParameter
                {
                    Type = "file",
                    Name = paramName,
                    In = "formData"
                };
                operation.Parameters.Add(fileParam);
            }

            operation.Consumes = new List<string> { "multipart/form-data" };
        }
    }
}
