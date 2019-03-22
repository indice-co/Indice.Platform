using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Indice.AspNetCore.Swagger
{
    /// <summary>
    /// Searches all requests and responses to find a direct reference of a base type and makes it accept any of the desendants.
    /// </summary>
    public class PolymorphicOperationFilter : IOperationFilter
    {
        /// <summary>
        /// Constructs the operation filter sharing finctionality with <see cref="PolymorphicSchemaFilter"/>
        /// </summary>
        /// <param name="configuration"></param>
        public PolymorphicOperationFilter(PolymorphicSchemaFilter configuration) {
            Configuration = configuration;
            EnumerableOfBaseType = typeof(IEnumerable<>).MakeGenericType(Configuration.BaseType);
        }

        private PolymorphicSchemaFilter Configuration { get; }   
        private Type EnumerableOfBaseType { get; }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="context"></param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context) {
            //if (context.ApiDescription.SupportedResponseTypes.Any(x => x.Type == Configuration.BaseType || EnumerableOfBaseType.IsAssignableFrom(x.Type))) {
            //    foreach (var contenType in operation.Responses) {
            //        var response = contenType.Value;
            //        if (response.Reference?.Id == Configuration.BaseType.Name) {
            //        }
            //    }
            //}
            if (context.ApiDescription.ActionDescriptor.Parameters.Any(x => x.ParameterType == Configuration.BaseType || EnumerableOfBaseType.IsAssignableFrom(x.ParameterType))) {
                foreach (var contenType in operation.RequestBody.Content) {
                    var request = contenType.Value.Schema;
                    if (request.Reference?.Id == Configuration.BaseType.Name ||
                        request.Items?.Reference?.Id == Configuration.BaseType.Name) {
                        if (request.Reference?.Id == Configuration.BaseType.Name) {
                            request.Reference = null;
                            request.OneOf = Configuration.AllOfReferences;
                        } else {
                            request.Items.Reference = null;
                            request.Items.AnyOf = Configuration.AllOfReferences;
                        }
                    }
                }
            }
        }
    }
}
