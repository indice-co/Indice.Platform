﻿using Microsoft.AspNetCore.Http;
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
            var contentToChange = operation.RequestBody.Content["multipart/form-data"];
            var formFileNames = typeof(IFormFile).GetProperties().Select(x => x.Name).ToArray();
            var paramsToRemove = contentToChange.Schema.Properties.Where(prop => formFileNames.Contains(prop.Key)).ToList();
            paramsToRemove.ForEach(x => contentToChange.Schema.Properties.Remove(x));
            paramsToRemove.ForEach(x => contentToChange.Encoding.Remove(x.Key));
            foreach (var paramName in allFileParamNames) {
                contentToChange.Schema.Properties.Add(paramName, new OpenApiSchema {
                    Type = "file"
                });
                contentToChange.Encoding.Add(paramName, new OpenApiEncoding {
                    Style = ParameterStyle.Form
                });
            }
        }
    }
}
