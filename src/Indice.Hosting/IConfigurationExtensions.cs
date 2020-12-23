namespace Microsoft.Extensions.Configuration
{
    internal static class IConfigurationExtensions
    {
        public static bool StopWorkerHost(this IConfiguration configuration) => configuration.GetSection("General").GetValue<bool>("StopWorkerHost") || configuration.GetValue<bool>("StopWorkerHost");
    }
}
