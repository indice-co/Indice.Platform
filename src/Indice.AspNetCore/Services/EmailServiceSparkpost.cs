using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;

namespace Indice.Services
{

    public class EmailServiceSparkPostSettings
    {
        public const string Name = "SparkPost";
        public string Sender { get; set; }
        public string ApiKey { get; set; }
        public string Api { get; set; }
    }

    /// <summary>
    /// Spark post implementation for the email service abstraction.
    /// https://developers.sparkpost.com/api/transmissions.html
    /// </summary>
    public class EmailServiceSparkpost : IEmailService
    {
        private readonly EmailServiceSparkPostSettings _settings;
        private readonly ICompositeViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EmailServiceSparkpost(EmailServiceSparkPostSettings settings, ICompositeViewEngine viewEngine, ITempDataProvider tempDataProvider, IHttpContextAccessor httpContextAccessor) {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task SendAsync(string[] recipients, string subject, string body) => await SendAsync<object>(recipients, subject, body, "Email", null);

        public async Task SendAsync<TModel>(string[] recipients, string subject, string body, string template, TModel data) where TModel : class {
            using (var httpClient = new HttpClient()) {
                httpClient.BaseAddress = new Uri(_settings.Api);
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(_settings.ApiKey);

                var request = new {
                    content = new {
                        from = _settings.Sender,
                        subject,
                        // text = "Testing SparkPost - the most awesomest email service in the world!",
                        html = await GetHtmlAsync<TModel>(body, subject, template.ToString(), data)
                    },
                    recipients = recipients.Select(recipient => new { address = recipient }).ToArray()
                };

                var response = await httpClient.PostAsync("transmissions", new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode) {
                    // Should log something.
                }
            }
        }

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

    public static class EmailServiceExtensions
    {
        public static async Task SendAsync(this IEmailService emailService, string recipient, string subject, string body) =>
            await emailService.SendAsync<object>(new string[] { recipient }, subject, body, "Email", null);

        public static async Task SendAsync<TModel>(this IEmailService emailService, string recipient, string subject, string body, string template, TModel data) where TModel : class =>
            await emailService.SendAsync(new string[] { recipient }, subject, body, template, data);
    }
}
