using System.ClientModel;
using System.Security.Claims;
using Azure.AI.OpenAI;
using Indice.Features.Agents.Core;
using Indice.Features.Agents.Core.Data;
using Indice.Features.Agents.Core.Workflows;
using Indice.Features.Agents.Core.Workflows.Abstractions;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Indice.Features.Agents.Core.Workflows.Usage;
using Indice.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extensions to configure the Agents feature.</summary>
public static class AgentsFeatureExtensions
{
    /// <summary>
    /// Registers Dex core services: <see cref="AgentsOptions"/> (bound from the <c>Dex</c> configuration section),
    /// <see cref="AzureOpenAIClient"/> (singleton — each pipeline step builds its own role-bound agent from it),
    /// the embedding generator, the <see cref="AgentsDbContext"/> wired to SQL Server, and <see cref="IDexRunner"/>.
    /// </summary>
    public static IServiceCollection AddAgentsCore(this IServiceCollection services, IConfiguration configuration, Action<AgentsOptions>? configureAction = null) {
        var optionsBuilder = services.AddOptions<AgentsOptions>().BindConfiguration("Dex");
        if (configureAction is not null) {
            optionsBuilder.Configure(configureAction);
        }
        services.AddSingleton<IValidateOptions<AgentsOptions>, AgentsOptionsValidator>();
        optionsBuilder.ValidateOnStart();

        services.TryAddSingleton(sp => {
            var opts = sp.GetRequiredService<IOptions<AgentsOptions>>().Value.AzureOpenAI;
            return new AzureOpenAIClient(new Uri(opts.Endpoint!), new ApiKeyCredential(opts.ApiKey!));
        });

        services.TryAddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp => {
            var opts = sp.GetRequiredService<IOptions<AgentsOptions>>().Value.AzureOpenAI;
            var client = sp.GetRequiredService<AzureOpenAIClient>();
            return client
                .GetEmbeddingClient(opts.Deployments.Embedding!)
                .AsIEmbeddingGenerator();
        });

        services.AddDbContext<AgentsDbContext>((sp, options) => {
            var opts = sp.GetRequiredService<IOptions<AgentsOptions>>().Value;
            var configureDbContext = opts.ConfigureDbContext ?? (
                (sp, dbContextOptions) => {
                    var connectionString = configuration.GetConnectionString("DexDb")
                        ?? throw new InvalidOperationException("Connection string 'DexDb' is not configured.");
                    dbContextOptions.UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "dex"));
                });
            configureDbContext.Invoke(sp, options);
        });

        // Request-scoped: the reasoning steps (which wrap their chat client with UsageTrackingChatClient) and
        // DexRunner resolve the same instance within a request, so DexRunner reads the run's accumulated usage.
        services.TryAddScoped<TokenUsageAccumulator>();
        services.TryAddTransient<UserClaimsAIContextProvider>();
        services.TryAddSingleton<IPromptTemplateRenderer, FileSystemPromptTemplateRenderer>();
        services.TryAddTransient<IDexRunner, DexRunner>();
        services.TryAddSingleton<WorkflowClaimsPrincipalSelector>(sp =>
            () => new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(BasicClaimTypes.Name, "Guest")],"DexInternal", BasicClaimTypes.Name, BasicClaimTypes.Role)
            )
        );

        services.AddDefaultDexPipeline();
        return services;
    }


}
