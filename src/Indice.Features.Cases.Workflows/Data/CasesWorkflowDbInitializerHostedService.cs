using Elsa.Models;
using System.Threading;
using Elsa.Persistence.EntityFramework.Core;
using Elsa.Serialization;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Workflows.Data;

/// <summary>
/// This service will be registered only if hosting environment is set at <strong>Developement</strong> in order to ensure the database is created.
/// </summary>
internal class CasesWorkflowDbInitializerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<CasesWorkflowDbInitializerHostedService> _logger;
    private readonly IContentSerializer _contentSerializer;
    /// <summary>
    /// Creates a new instance of <see cref="CasesWorkflowDbInitializerHostedService"/>
    /// </summary>
    /// <param name="serviceScopeFactory">The service provider factory. Used to create scopes</param>
    /// <param name="environment">The service environment</param>
    /// <param name="logger">a logger</param>
    /// <param name="contentSerializer">Elsas content serializer</param>
    /// <exception cref="ArgumentNullException"></exception>
    public CasesWorkflowDbInitializerHostedService(IServiceScopeFactory serviceScopeFactory, IHostEnvironment environment, ILogger<CasesWorkflowDbInitializerHostedService> logger, IContentSerializer contentSerializer) {
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _contentSerializer = contentSerializer ?? throw new ArgumentNullException(nameof(contentSerializer));
    }

    /// <summary>
    /// Executes the background service's logic.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token.</param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        if (!_environment.IsDevelopment()) {
            return;
        }
        try {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ElsaContext>();
            var seedOptions = scope.ServiceProvider.GetRequiredService<IOptions<CasesWorkflowDbInitializerOptions>>();
            await dbContext.InitializeAsync(seedOptions, _contentSerializer);
            await Task.Delay(TimeSpan.FromSeconds(2));
            if (!await dbContext.WorkflowDefinitions.AnyAsync(x => x.Name == "HelloWorld"))
                await SeedSampleWorkflowDefinition(scope, stoppingToken);
        } catch (Exception ex) {
            _logger.LogError(ex, "Failed to run CasesWorkflowDbInitializer");
        }
    }

    private static async Task SeedSampleWorkflowDefinition(IServiceScope scope, CancellationToken stoppingToken) {
        var _workflowPublisher = scope.ServiceProvider.GetRequiredService<IWorkflowPublisher>();
        var _contentSerializer = scope.ServiceProvider.GetRequiredService<IContentSerializer>();
        var _tenantAccessor = scope.ServiceProvider.GetRequiredService<ITenantAccessor>();


        var json = """
{
  "$id": "1",
  "definitionId": "5623dbb6c37a4cffbbacd5c27d83cc83",
  "versionId": "c01fc75198184463aced98f40df7b9d8",
  "name": "HelloWorld",
  "displayName": "Hello World",
  "version": 1,
  "variables": { "$id": "2", "data": {} },
  "customAttributes": { "$id": "3", "data": {} },
  "isSingleton": false,
  "persistenceBehavior": "WorkflowBurst",
  "deleteCompletedInstances": false,
  "isPublished": true,
  "isLatest": true,
  "activities": [
    {
      "$id": "4",
      "activityId": "0641c8d5-9f5d-49c5-b871-3add4bd22d3b",
      "type": "HttpEndpoint",
      "displayName": "HTTP Endpoint",
      "persistWorkflow": false,
      "loadWorkflowContext": false,
      "saveWorkflowContext": false,
      "properties": [
        { "$id": "5", "name": "Path", "expressions": { "$id": "6", "Literal": "/hello-world" } },
        { "$id": "7", "name": "Methods", "expressions": { "$id": "8", "Json": "[\"GET\"]" } },
        { "$id": "9", "name": "ReadContent", "expressions": { "$id": "10" } },
        { "$id": "11", "name": "TargetType", "expressions": { "$id": "12" } }
      ],
      "propertyStorageProviders": {}
    },
    {
      "$id": "13",
      "activityId": "c0a8c3d3-7e23-4630-ab85-3104ff50777b",
      "type": "WriteHttpResponse",
      "displayName": "HTTP Response",
      "persistWorkflow": false,
      "loadWorkflowContext": false,
      "saveWorkflowContext": false,
      "properties": [
        { "$id": "14", "name": "Content", "expressions": { "$id": "15", "Literal": "Hello World!" } },
        { "$id": "16", "name": "ContentType", "expressions": { "$id": "17" } },
        { "$id": "18", "name": "StatusCode", "expressions": { "$id": "19" } },
        { "$id": "20", "name": "CharSet", "expressions": { "$id": "21" } },
        { "$id": "22", "name": "ResponseHeaders", "expressions": { "$id": "23" } }
      ],
      "propertyStorageProviders": {}
    }
  ],
  "connections": [
    {
      "$id": "24",
      "sourceActivityId": "0641c8d5-9f5d-49c5-b871-3add4bd22d3b",
      "targetActivityId": "c0a8c3d3-7e23-4630-ab85-3104ff50777b",
      "outcome": "Done"
    }
  ],
  "id": "c01fc75198184463aced98f40df7b9d8"
}
""";
        var postedModel = _contentSerializer.Deserialize<WorkflowDefinition>(json);
        var workflowDefinition = await _workflowPublisher.GetDraftAsync(postedModel.DefinitionId, stoppingToken) ?? _workflowPublisher.New();
        workflowDefinition.Activities = postedModel.Activities;
        workflowDefinition.Channel = postedModel.Channel;
        workflowDefinition.Connections = postedModel.Connections;
        workflowDefinition.Description = postedModel.Description;
        workflowDefinition.Name = postedModel.Name;
        workflowDefinition.Tag = postedModel.Tag;
        workflowDefinition.Variables = postedModel.Variables;
        workflowDefinition.ContextOptions = postedModel.ContextOptions;
        workflowDefinition.CustomAttributes = postedModel.CustomAttributes;
        workflowDefinition.DisplayName = postedModel.DisplayName;
        workflowDefinition.IsSingleton = postedModel.IsSingleton;
        workflowDefinition.DeleteCompletedInstances = postedModel.DeleteCompletedInstances;
        workflowDefinition.PersistenceBehavior = postedModel.PersistenceBehavior;
        workflowDefinition.TenantId = await _tenantAccessor.GetTenantIdAsync();

        var wf = await _workflowPublisher.SaveDraftAsync(workflowDefinition, stoppingToken);
        await _workflowPublisher.PublishAsync(wf.DefinitionId, stoppingToken);
    }
}