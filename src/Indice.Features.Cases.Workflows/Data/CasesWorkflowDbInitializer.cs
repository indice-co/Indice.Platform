using Elsa.Models;
using Elsa.Persistence.EntityFramework.Core;
using Elsa.Serialization;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Workflows.Data;

/// <summary>Cases DbContext intialization extensions</summary>
public static class CasesDbInitalizerExtesnions
{
    /// <summary>
    /// Create database if not exists and seed with initial data
    /// </summary>
    /// <param name="dbContext">The database context</param>
    /// <param name="options">Seed options</param>
    /// <param name="contentSerializer">Elsas content serializer</param>
    /// <param name="serviceScope"></param>
    /// <returns>The Task</returns>
    public async static Task InitializeAsync(
        this ElsaContext dbContext,
        IOptions<CasesWorkflowDbInitializerOptions> options,
        IContentSerializer contentSerializer,
        IServiceScope serviceScope) {
        if (await dbContext.Database.EnsureCreatedAsync()) {
            await dbContext.SeedAsync(options, contentSerializer);
            await SeedAdditionalAsync(options, contentSerializer, serviceScope);
        }
    }

    /// <summary>Seeds additional client workflows from Json.</summary>
    public async static Task SeedAdditionalAsync(
        IOptions<CasesWorkflowDbInitializerOptions> options,
        IContentSerializer contentSerializer,
        IServiceScope serviceScope
    ) {
        if (options.Value.WorkflowDefinitions.Count == 0) {
            return;
        }
        
        var workflowPublisher = serviceScope.ServiceProvider.GetRequiredService<IWorkflowPublisher>();
        var tenantAccessor = serviceScope.ServiceProvider.GetRequiredService<ITenantAccessor>();

        foreach (var postedModel in options.Value.WorkflowDefinitions.Select(contentSerializer.Deserialize<WorkflowDefinition>)) {
            var workflowDefinition = await workflowPublisher.GetDraftAsync(postedModel.DefinitionId) ?? workflowPublisher.New();
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
            workflowDefinition.TenantId = await tenantAccessor.GetTenantIdAsync();
            
            var wf = await workflowPublisher.SaveDraftAsync(workflowDefinition);
            await workflowPublisher.PublishAsync(wf.DefinitionId);
        }
        
    }

    /// <summary>
    /// Seed the database to its initial state
    /// </summary>
    /// <param name="dbContext">The database context</param>
    /// <param name="options">Seed options</param>
    /// <param name="contentSerializer">Elsas content serializer</param>
    /// <returns></returns>
    public async static Task SeedAsync(this ElsaContext dbContext, IOptions<CasesWorkflowDbInitializerOptions> options, IContentSerializer contentSerializer) {
        WorkflowDefinition workflowDefinition =
          new WorkflowDefinition() {
              Id = "c01fc75198184463aced98f40df7b9d8",
              DefinitionId = "21ce61293f19428a9801f0e0d31eb415",
              Name = "HelloWorldDB",
              DisplayName = "Hello World DB",
              Version = 1,
              IsSingleton = false,
              PersistenceBehavior = WorkflowPersistenceBehavior.WorkflowBurst,
              DeleteCompletedInstances = false,
              IsPublished = true,
              IsLatest = true,
              Tag = "SampleAddress",
              Activities = new List<ActivityDefinition>() {
                new ActivityDefinition() {
                    ActivityId = "0641c8d5-9f5d-49c5-b871-3add4bd22d3b",
                    Type = "HttpEndpoint",
                    DisplayName = "HTTP Endpoint",
                    PersistWorkflow = false,
                    LoadWorkflowContext = false,
                    SaveWorkflowContext = false,
                    Properties = new List<ActivityDefinitionProperty>() {
                        new ActivityDefinitionProperty() {
                            Name = "Path",
                            Expressions = new Dictionary<string,string?>(){
                                ["Id"] = "6",
                                ["Literal"] = "/hello-world-db"
                            }
                        },
                        new ActivityDefinitionProperty() {
                            Name = "Methods",
                            Expressions = new Dictionary<string,string?>() {
                                ["Id"] = "8",
                                ["Json"] = "[\"GET\"]"
                            }
                        },
                        new ActivityDefinitionProperty() {

                            Name = "ReadContent",
                            Expressions = new Dictionary<string,string?>() {
                                ["Id"] = "10"
                            }
                        },
                        new ActivityDefinitionProperty() {
                            Name = "TargetType",
                            Expressions = new Dictionary<string,string?>() {
                                ["Id"] = "12"
                            }
                        }
                    }
                },
                new ActivityDefinition() {
                    ActivityId = "c0a8c3d3-7e23-4630-ab85-3104ff50777b",
                    Type = "WriteHttpResponse",
                    DisplayName = "HTTP Response",
                    PersistWorkflow = false,
                    LoadWorkflowContext = false,
                    SaveWorkflowContext = false,
                    Properties = new List<ActivityDefinitionProperty>() {
                        new ActivityDefinitionProperty() {
                            Name = "Content",
                            Expressions = new Dictionary<string,string?>() {
                                ["Id"] = "15",
                                ["Literal"] = "Hello World DB instantiated!"
                            }
                        },
                        new ActivityDefinitionProperty() {
                            Name = "ContentType",
                            Expressions = new Dictionary<string,string?>() {
                                ["Id"] = "17"
                            }
                        },
                        new ActivityDefinitionProperty() {
                            Name = "StatusCode",
                            Expressions = new Dictionary<string,string?>() {
                                ["Id"] = "19"
                            }
                        },
                        new ActivityDefinitionProperty() {
                            Name = "CharSet",
                            Expressions = new Dictionary<string,string?>() {
                                ["Id"] = "21"
                            }
                        },
                        new ActivityDefinitionProperty() {
                            Name = "ResponseHeaders",
                            Expressions = new Dictionary<string,string?>() {
                                ["Id"] = "23"
                            }
                        }
                    },
                }
              },
              Connections = [ new ConnectionDefinition(){
                SourceActivityId = "0641c8d5-9f5d-49c5-b871-3add4bd22d3b",
                TargetActivityId = "c0a8c3d3-7e23-4630-ab85-3104ff50777b",
                Outcome =  "Done"
              }],
              //CreatedAt = NodaTime.Instant.FromDateTimeUtc(DateTime.Now),
          };
        var data = new {
            workflowDefinition.Activities,
            workflowDefinition.Connections,
            workflowDefinition.Variables,
            workflowDefinition.ContextOptions,
            workflowDefinition.CustomAttributes,
            workflowDefinition.Channel
        };
        dbContext.WorkflowDefinitions.Add(workflowDefinition);
        var json = contentSerializer.Serialize(data);
        dbContext.Entry(workflowDefinition).Property("Data").CurrentValue = json;
        await dbContext.SaveChangesAsync();
    }

}
