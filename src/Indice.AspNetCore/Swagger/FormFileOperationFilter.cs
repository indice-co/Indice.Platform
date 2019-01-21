using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Dynamic;
using Microsoft.OpenApi.Models;

namespace Indice.AspNetCore.Swagger
{
    internal class FormFileOperationFilter : IOperationFilter
    {

        public void Apply(OpenApiOperation operation, OperationFilterContext context) {
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

            
            if (!operation.RequestBody.Content.ContainsKey("multipart/form-data")) {
                return;
            }
            var schemaToChange = operation.RequestBody.Content["multipart/form-data"].Schema;
            var formFileNames = typeof(IFormFile).GetProperties().Select(x => x.Name).ToArray();
            var paramsToRemove = schemaToChange.Properties.Where(prop => formFileNames.Contains(prop.Key)).ToList();
            paramsToRemove.ForEach(x => schemaToChange.Properties.Remove(x));
            foreach (var paramName in allFileParamNames) {
                schemaToChange.Properties.Add(paramName, new OpenApiSchema {
                    Type = "file"
                });
            }
        }
    }
}
