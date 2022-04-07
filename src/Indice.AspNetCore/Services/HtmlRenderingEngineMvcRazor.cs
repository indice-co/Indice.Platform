using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace Indice.Services
{
    /// <summary>
    /// Rendering engine. This one is using AspNetCore MVC Razor native engine. This cannot work without mvc
    /// </summary>
    public class HtmlRenderingEngineMvcRazor : IHtmlRenderingEngine
    {
        private readonly ICompositeViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;
        /// <summary>
        /// Constructs the service.
        /// </summary>
        /// <param name="viewEngine">Represents an <see cref="IViewEngine"/> that delegates to one of a collection of view engines.</param>
        /// <param name="tempDataProvider">Defines the contract for temporary-data providers that store data that is viewed on the next request.</param>
        /// <param name="httpContextAccessor">Used to access the <see cref="HttpContext"/> through the <see cref="IHttpContextAccessor"/> interface and its default implementation <see cref="HttpContextAccessor"/>.</param>
        /// <param name="serviceProvider">Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</param>
        public HtmlRenderingEngineMvcRazor(
            ICompositeViewEngine viewEngine,
            ITempDataProvider tempDataProvider,
            IHttpContextAccessor httpContextAccessor,
            IServiceProvider serviceProvider
        ) {
            _viewEngine = viewEngine ?? throw new ArgumentNullException(nameof(viewEngine));
            _tempDataProvider = tempDataProvider ?? throw new ArgumentNullException(nameof(tempDataProvider));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <inheritdoc />
        public async Task<string> RenderAsync(string template, Type dataType, object data) {
            var html = string.Empty;
            var viewName = template;
            var viewDataDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) {
                
            };
            switch (data) {
                case string text:
                    viewDataDictionary.Model = text;
                    break;
                case null:
                    viewDataDictionary.Model = null;
                    break;
                default:
                    viewDataDictionary.Model = ToExpandoObject(dataType, data);
                    break;
            }
            var actionContext = GetActionContext();
            var tempDataDictionary = new TempDataDictionary(actionContext.HttpContext, _tempDataProvider);
            using (var writer = new StringWriter()) {
                var viewResult = _viewEngine.FindView(actionContext, viewName, false);
                var viewContext = new ViewContext(actionContext, viewResult.View, viewDataDictionary, tempDataDictionary, writer, new HtmlHelperOptions());
                await viewResult.View.RenderAsync(viewContext);
                html = writer.GetStringBuilder().ToString();
            }
            return html;
        }
        private ActionContext GetActionContext() {
            // HttpContext can be null when IEmailService is used outside of the ASP.NET Core request pipeline (e.x a background service).
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) {
                httpContext = new DefaultHttpContext {
                    RequestServices = _serviceProvider
                };
            }
            return new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        }

        private static ExpandoObject ToExpandoObject(Type dataType, object value) {
           var obj = new ExpandoObject() as IDictionary<string, object>;
            foreach (var property in dataType.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
                obj.Add(property.Name, property.GetValue(value, null));
            }
            return obj as ExpandoObject;
        }
    }
}
