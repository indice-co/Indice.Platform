using Indice.Abstractions;
using Indice.Configuration;
using Indice.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddIndiceServices(this IServiceCollection services, IConfiguration configuration) {

            services.Configure<GeneralSettings>(configuration.GetSection(GeneralSettings.Name));

            return services;
        }

        public static IServiceCollection AddEmailServiceSparkpost(this IServiceCollection services, IConfiguration configuration) {
            services.Configure<EmailServiceSparkPostSettings>(configuration.GetSection(EmailServiceSparkPostSettings.Name));
            services.AddTransient((sp) => sp.GetRequiredService<IOptions<EmailServiceSparkPostSettings>>().Value);
            services.AddTransient<IEmailService, EmailServiceSparkpost>();
            return services;
        }

        public static IServiceCollection AddEmailService(this IServiceCollection services, IConfiguration configuration) {
            services.Configure<EmailServiceSettings>(configuration.GetSection(EmailServiceSettings.Name));
            services.AddTransient((sp) => sp.GetRequiredService<IOptions<EmailServiceSettings>>().Value);
            services.AddTransient<IEmailService, EmailServiceSmtp>();
            return services;
        }

        public static IServiceCollection AddSmsServiceYouboto(this IServiceCollection services, IConfiguration configuration) {
            services.Configure<SmsServiceSettings>(configuration.GetSection(SmsServiceSettings.Name));
            services.AddTransient((sp) => sp.GetRequiredService<IOptions<SmsServiceSettings>>().Value);
            services.AddTransient<ISmsService, SmsServiceYuboto>();
            return services;
        }

        public static IServiceCollection AddFilesAzure(this IServiceCollection services) {

            services.AddTransient<IFileService, FileServiceAzureStorage>((sp) => new FileServiceAzureStorage(sp.GetRequiredService<IConfiguration>().GetConnectionString(FileServiceAzureStorage.CONNECTION_STRING_NAME),
                                                                                                             sp.GetRequiredService<IHostingEnvironment>().EnvironmentName));
            return services;
        }

        public static IServiceCollection AddFilesInMemory(this IServiceCollection services) {

            services.AddTransient<IFileService, FileServiceInMemory>();
            return services;
        }

        public static IServiceCollection AddMarkdown(this IServiceCollection services) {
            services.AddTransient<IMarkdownProcessor, MarkdigProcessor>();
            return services;
        }
    }
}
