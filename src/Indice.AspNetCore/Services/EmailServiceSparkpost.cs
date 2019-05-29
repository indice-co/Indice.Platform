using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Indice.Configuration;
using Indice.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Indice.Services
{
    /// <summary>
    /// Custom settings that are used to send emails via SparkPost.
    /// </summary>
    public class EmailServiceSparkPostSettings
    {
        /// <summary>
        /// The config section name.
        /// </summary>
        public const string Name = "SparkPost";

        /// <summary>
        /// The default sender address (ex. no-reply@indice.gr).
        /// </summary>
        public string Sender { get; set; }

        /// <summary>
        /// The SparkPost API key.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// The SparkPost API URL (ex. https://api.sparkpost.com/api/v1/).
        /// </summary>
        public string Api { get; set; } = "https://api.sparkpost.com/api/v1/";
    }

    /// <summary>
    /// SparkPost implementation for the email service abstraction.
    /// https://developers.sparkpost.com/api/transmissions.html
    /// </summary>
    public class EmailServiceSparkpost : EmailServiceRazorBase
    {
        private readonly EmailServiceSparkPostSettings _settings;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// constructs the service
        /// </summary>
        /// <param name="settings">An instance of <see cref="EmailServiceSparkPostSettings"/> used to initialize the service.</param>
        /// <param name="viewEngine">Represents an <see cref="IViewEngine"/> that delegates to one of a collection of view engines.</param>
        /// <param name="tempDataProvider">Defines the contract for temporary-data providers that store data that is viewed on the next request.</param>
        /// <param name="httpContextAccessor">Used to access the <see cref="HttpContext"/> through the <see cref="IHttpContextAccessor"/> interface and its default implementation <see cref="HttpContextAccessor"/>.</param>
        /// <param name="httpClient">The http client to use (DI managed)</param>
        public EmailServiceSparkpost(EmailServiceSparkPostSettings settings, ICompositeViewEngine viewEngine, ITempDataProvider tempDataProvider, IHttpContextAccessor httpContextAccessor, HttpClient httpClient)
            : base(viewEngine, tempDataProvider, httpContextAccessor) {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            if (_httpClient.BaseAddress == null) {
                _httpClient.BaseAddress = new Uri(_settings.Api);
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(_settings.ApiKey);
            }
        }

        /// <inheritdoc/>
        public override async Task SendAsync<TModel>(string[] recipients, string subject, string body, string template, TModel data, FileAttachment[] attachments = null) {
            
            var request = new SparkPostRequest {
                Content = new SparkPostContent {
                    From = _settings.Sender,
                    Subject = subject,
                    Html = await GetHtmlAsync(body, subject, template.ToString(), data)
                },
                Recipients = recipients.Select(recipient => new SparkPostRecipient {
                    Address = recipient
                }).ToArray()
            };
            if (attachments?.Length > 0) {
                var attachmentsList = new List<SparkPostAttachment>();
                foreach (var attachment in attachments) {
                    attachmentsList.Add(new SparkPostAttachment {
                        Name = attachment.FileName,
                        Type = FileExtensions.GetMimeType(Path.GetExtension(attachment.FileName)),
                        Data = Convert.ToBase64String(attachment.Data)
                    });
                }
                request.Content.Attachments = attachmentsList.ToArray();
            }
            var requestJson = JsonConvert.SerializeObject(request, new JsonSerializerSettings {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
            var response = await _httpClient.PostAsync("transmissions", new StringContent(requestJson, Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode) {
                // Should log something.
            }
        }
    }

    #region SparkPost Models
    internal class SparkPostRequest
    {
        public SparkPostContent Content { get; set; }
        public SparkPostRecipient[] Recipients { get; set; }
    }

    internal class SparkPostContent
    {
        public string From { get; set; }
        public string Subject { get; set; }
        public string Html { get; set; }
        public SparkPostAttachment[] Attachments { get; set; }
    }

    internal class SparkPostRecipient
    {
        public string Address { get; set; }
    }

    internal class SparkPostAttachment
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Data { get; set; }
    }
    #endregion
}
