using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Indice.Services;
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
    /// A variant of the <see cref="IEmailService"/> that uses Razor virews to render the emails.
    /// </summary>
    public abstract class EmailServiceRazorBase : IEmailService
    {
        private readonly ICompositeViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;


        /// <summary>
        /// constructs the service
        /// </summary>
        /// <param name="viewEngine"></param>
        /// <param name="tempDataProvider"></param>
        /// <param name="httpContextAccessor"></param>
        public EmailServiceRazorBase(ICompositeViewEngine viewEngine, ITempDataProvider tempDataProvider, IHttpContextAccessor httpContextAccessor) {
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Sends an email. using the default email template named "Email".
        /// </summary>
        /// <param name="recipients">The recipients of the email message.</param>
        /// <param name="subject">The subject of the email message.</param>
        /// <param name="body">The body of the email message.</param>
        /// <returns></returns>
        public async Task SendAsync(string[] recipients, string subject, string body) => await SendAsync<object>(recipients, subject, body, "Email", null);
        
        /// <summary>
        /// Sends an email.
        /// </summary>
        /// <typeparam name="TModel">The type of the <paramref name="data"/> that will be applied to the template.</typeparam>
        /// <param name="recipients">The recipients of the email message.</param>
        /// <param name="subject">The subject of the email message.</param>
        /// <param name="body">The body of the email message.</param>
        /// <param name="template">The template of the email message.</param>
        /// <param name="data">The data model that contains information to render in the email message.</param>
        /// <returns></returns>
        public abstract Task SendAsync<TModel>(string[] recipients, string subject, string body, string template, TModel data) where TModel : class;

        /// <summary>
        /// Generates the email body using the Razor engine.
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="body"></param>
        /// <param name="subject"></param>
        /// <param name="template"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual async Task<string> GetHtmlAsync<TModel>(string body, string subject, string template, TModel data) where TModel : class {
            var html = body;
            var viewName = template;

            var viewDataDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) {
                { "Subject", subject },
                { "Body", body }
            };

            viewDataDictionary.Model = data != null ? ToExpandoObject(data) : null;
            var actionContext = new ActionContext(_httpContextAccessor.HttpContext, new RouteData(), new ActionDescriptor());
            var tempDataDictionary = new TempDataDictionary(_httpContextAccessor.HttpContext, _tempDataProvider);

            using (var writer = new System.IO.StringWriter()) {
                var viewResult = _viewEngine.FindView(actionContext, viewName, false);
                var viewContext = new ViewContext(actionContext, viewResult.View, viewDataDictionary, tempDataDictionary, writer, new HtmlHelperOptions());
                await viewResult.View.RenderAsync(viewContext);
                html = writer.GetStringBuilder().ToString();
            }

            return html;
        }

        private static ExpandoObject ToExpandoObject<T>(T value) {
            var obj = new ExpandoObject() as IDictionary<string, object>;

            foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
                obj.Add(property.Name, property.GetValue(value, null));
            }

            return obj as ExpandoObject;
        }
    }
}
