using Indice.Configuration;
using Indice.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Extensions
{
    /// <summary>
    /// Extensions to configura the Service collection of an aspnet core app.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Indice common services 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddIndiceServices(this IServiceCollection services, IConfiguration configuration) {

            services.Configure<GeneralSettings>(configuration.GetSection(GeneralSettings.Name));

            return services;
        }

        /// <summary>
        /// Adds EmailService that uses Sparkpost to send and razor templates
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddEmailServiceSparkpost(this IServiceCollection services, IConfiguration configuration) {
            services.Configure<EmailServiceSparkPostSettings>(configuration.GetSection(EmailServiceSparkPostSettings.Name));
            services.AddTransient((sp) => sp.GetRequiredService<IOptions<EmailServiceSparkPostSettings>>().Value);
            services.AddTransient<IEmailService, EmailServiceSparkpost>();
            return services;
        }

        /// <summary>
        /// Adds EmailService using smtp settings in configuration.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddEmailService(this IServiceCollection services, IConfiguration configuration) {
            services.Configure<EmailServiceSettings>(configuration.GetSection(EmailServiceSettings.Name));
            services.AddTransient((sp) => sp.GetRequiredService<IOptions<EmailServiceSettings>>().Value);
            services.AddTransient<IEmailService, EmailServiceSmtp>();
            return services;
        }

        /// <summary>
        /// Adds SmsService using Youboto
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddSmsServiceYouboto(this IServiceCollection services, IConfiguration configuration) {
            services.Configure<SmsServiceSettings>(configuration.GetSection(SmsServiceSettings.Name));
            services.AddTransient((sp) => sp.GetRequiredService<IOptions<SmsServiceSettings>>().Value);
            services.AddTransient<ISmsService, SmsServiceYuboto>();
            return services;
        }

        /// <summary>
        /// Adds FileService using Azre blob storage as the backing store
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddFilesAzure(this IServiceCollection services) {

            services.AddTransient<IFileService, FileServiceAzureStorage>((sp) => new FileServiceAzureStorage(sp.GetRequiredService<IConfiguration>().GetConnectionString(FileServiceAzureStorage.CONNECTION_STRING_NAME),
                                                                                                             sp.GetRequiredService<IHostingEnvironment>().EnvironmentName));
            return services;
        }

        /// <summary>
        /// Adds FileService using inmemory storage as the backing store. Only for testing
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddFilesInMemory(this IServiceCollection services) {

            services.AddTransient<IFileService, FileServiceInMemory>();
            return services;
        }

        /// <summary>
        /// Adds Markdig as a Markdown processor. Needed to use with aspnet md tag helper.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddMarkdown(this IServiceCollection services) {
            services.AddTransient<IMarkdownProcessor, MarkdigProcessor>();
            return services;
        }
    }
}
