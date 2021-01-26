using System;
using System.Net.Http;
using Indice.Services;

namespace Indice.AspNetCore.Configuration
{
    /// <summary>
    /// Options for configuring <see cref="SmsServiceApifon"/>.
    /// </summary>
    public class SmsServiceApifonOptions
    {
        /// <summary>
        /// Optional options for <see cref="HttpClientHandler"/>
        /// </summary>
        public HttpClientHandler PrimaryHttpMessageHandler { get; set; }
    }
}
