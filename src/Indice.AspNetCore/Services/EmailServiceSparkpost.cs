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
    /// <summary>
    /// Custom settings that are used to send emails via SparkPost
    /// </summary>
    public class EmailServiceSparkPostSettings
    {
        /// <summary>
        /// The config section name.
        /// </summary>
        public const string Name = "SparkPost";

        /// <summary>
        /// The default sernder (ie no-reply@indice.gr)
        /// </summary>
        public string Sender { get; set; }

        /// <summary>
        /// The SparkPost API key
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// The SparkPost API url (ie https://api.sparkpost.com/api/v1/)
        /// </summary>
        public string Api { get; set; } = "https://api.sparkpost.com/api/v1/";
    }

    /// <summary>
    /// Spark post implementation for the email service abstraction.
    /// https://developers.sparkpost.com/api/transmissions.html
    /// </summary>
    public class EmailServiceSparkpost : EmailServiceRazorBase
    {
        private readonly EmailServiceSparkPostSettings _settings;
        
        /// <summary>
        /// constructs the service
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="viewEngine"></param>
        /// <param name="tempDataProvider"></param>
        /// <param name="httpContextAccessor"></param>
        public EmailServiceSparkpost(EmailServiceSparkPostSettings settings, ICompositeViewEngine viewEngine, ITempDataProvider tempDataProvider, IHttpContextAccessor httpContextAccessor)
            : base(viewEngine, tempDataProvider, httpContextAccessor) {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

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
        public override async Task SendAsync<TModel>(string[] recipients, string subject, string body, string template, TModel data) {
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
    }
}
