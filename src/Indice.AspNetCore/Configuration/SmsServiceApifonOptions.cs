using System;
using System.Net.Http;
using Indice.Services;
using Microsoft.Extensions.Http;

namespace Indice.AspNetCore.Configuration
{
    /// <summary>
    /// Options for configuring <see cref="SmsServiceApifon"/>.
    /// </summary>
    public class SmsServiceApifonOptions
    {
        /// <summary>
        /// Optional options for <see cref="HttpMessageHandler"/>
        /// </summary>
        public Func<IServiceProvider, HttpMessageHandler> ConfigurePrimaryHttpMessageHandler { get; set; }
    }
}
