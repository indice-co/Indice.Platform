- context.TryGetUser() on ActivityContext is not available anymore, use context.TryGetLastActor() optionally resolving with your identity provider
- Bookmark hashes will need recalculation as they are only calculated using `CaseId` and `ActionId` in the case of `AwaitActionActivity`. see script
- Remove `When` from activities that had `AuditMeta` as an input, this will be automatically set to the current DateTimeOffset i.e. AssignCaseToUserActivity - no breaking change 
- Case rejection reasons and approval resources should be copied to `Workflow` as well using `WorkflowSharedResource`
- `GetCaseDetails` activity has now the `FetchPublicData` input set to false by default. If this activity ran on your workflow on an http context user that was the Owner/Creator of the case then you probably need to set this to true.
- CasesManager `PatchData` call now has the `patchPublicData` argument to disambiguate if you want to update the public/private data. 
- Whenever possible avoid relying on last `Actor` context in your activities' logic. Whenever you can, be EXPLICIT about which action you want to perform some helper arguments have been added i.e. `FetchPublicData` when you want to get the public data or `PatchPublicData` when you want to patch the public data.


- The following queries must be ran on an existing elsa database to alter `Input` and `Output` `Activity` data:
```sql
-- 7.47 namespaces and older version
UPDATE [Elsa].[WorkflowInstances]
SET [data] = REPLACE([data], N'Indice.Features.Cases.Data.Models.AuditMeta, Indice.Features.Cases.AspNetCore', N'Indice.Features.Cases.Workflows.Integrations.AuditMeta, Indice.Features.Cases.Workflows')
WHERE PATINDEX(N'%Indice.Features.Cases.Data.Models.AuditMeta, Indice.Features.Cases.AspNetCore%', [data]) > 0

UPDATE [Elsa].[WorkflowInstances]
SET [data] = REPLACE([data], N'Indice.Features.Cases.Data.Models.ActionRequest, Indice.Features.Cases.AspNetCore', N'Indice.Features.Cases.Workflows.Models.InvokeActionRequest, Indice.Features.Cases.Workflows')
WHERE PATINDEX(N'%Indice.Features.Cases.Data.Models.ActionRequest, Indice.Features.Cases.AspNetCore%', [data]) > 0

UPDATE [Elsa].[WorkflowInstances]
SET [data] = REPLACE([data], N'Indice.Features.Cases.Models.ApprovalRequest, Indice.Features.Cases.AspNetCore', N'Indice.Features.Cases.Workflows.Models.InvokeApprovalRequest, Indice.Features.Cases.Workflows')
WHERE PATINDEX(N'%Indice.Features.Cases.Models.ApprovalRequest, Indice.Features.Cases.AspNetCore%', [data]) > 0

UPDATE [Elsa].[WorkflowInstances]
SET [data] = REPLACE([data], N'Indice.Features.Cases.Models.EditCaseRequest, Indice.Features.Cases.AspNetCore', N'Indice.Features.Cases.Workflows.Models.InvokeEditRequest, Indice.Features.Cases.Workflows')
WHERE PATINDEX(N'%Indice.Features.Cases.Models.EditCaseRequest, Indice.Features.Cases.AspNetCore%', [data]) > 0

UPDATE [Elsa].[WorkflowInstances]
SET [data] = REPLACE([data], N'Indice.Features.Cases.Models.Responses.Case, Indice.Features.Cases.AspNetCore', N'Indice.Features.Cases.Workflows.Integrations.Case, Indice.Features.Cases.Workflows')
WHERE PATINDEX(N'%Indice.Features.Cases.Models.Responses.Case, Indice.Features.Cases.AspNetCore%', [data]) > 0

-- previous rc version namespaces
UPDATE [Elsa].[WorkflowInstances]
SET [data] = REPLACE([data], N'Indice.Features.Cases.Core.Models.AuditMeta, Indice.Features.Cases.Core', N'Indice.Features.Cases.Workflows.Integrations.AuditMeta, Indice.Features.Cases.Workflows')
WHERE PATINDEX(N'%Indice.Features.Cases.Core.Models.AuditMeta, Indice.Features.Cases.Core%', [data]) > 0

UPDATE [Elsa].[WorkflowInstances]
SET [data] = REPLACE([data], N'Indice.Features.Cases.Core.Models.ActionRequest, Indice.Features.Cases.AspNetCore', N'Indice.Features.Cases.Workflows.Models.InvokeActionRequest, Indice.Features.Cases.Workflows')
WHERE PATINDEX(N'%Indice.Features.Cases.Core.Models.ActionRequest, Indice.Features.Cases.AspNetCore%', [data]) > 0

UPDATE [Elsa].[WorkflowInstances]
SET [data] = REPLACE([data], N'Indice.Features.Cases.Core.Models.ApprovalRequest, Indice.Features.Cases.AspNetCore', N'Indice.Features.Cases.Workflows.Models.InvokeApprovalRequest, Indice.Features.Cases.Workflows')
WHERE PATINDEX(N'%Indice.Features.Cases.Core.Models.ApprovalRequest, Indice.Features.Cases.AspNetCore%', [data]) > 0

UPDATE [Elsa].[WorkflowInstances]
SET [data] = REPLACE([data], N'Indice.Features.Cases.Core.Models.EditCaseRequest, Indice.Features.Cases.AspNetCore', N'Indice.Features.Cases.Workflows.Models.InvokeEditRequest, Indice.Features.Cases.Workflows')
WHERE PATINDEX(N'%Indice.Features.Cases.Core.Models.EditCaseRequest, Indice.Features.Cases.AspNetCore%', [data]) > 0

UPDATE [Elsa].[WorkflowInstances]
SET [data] = REPLACE([data], N'Indice.Features.Cases.Core.Models.Responses.Case, Indice.Features.Cases.AspNetCore', N'Indice.Features.Cases.Workflows.Integrations.Case, Indice.Features.Cases.Workflows')
WHERE PATINDEX(N'%Indice.Features.Cases.Core.Models.Responses.Case, Indice.Features.Cases.AspNetCore%', [data]) > 0
```

- The following queries must be ran on an existing elsa database to alter `Bookmark` data - fixes old as well:
```sql
-- Indice.Features.Cases.AspNetCore namespace 7.47 version
UPDATE [Elsa].[Bookmarks]
SET ModelType = REPLACE(ModelType,
    'Indice.Features.Cases.Workflows.Bookmarks.AwaitApproval.AwaitApprovalBookmark, Indice.Features.Cases.AspNetCore',
    'Indice.Features.Cases.Workflows.Bookmarks.AwaitApprovalBookmark, Indice.Features.Cases.Workflows')
WHERE ModelType = 'Indice.Features.Cases.Workflows.Bookmarks.AwaitApproval.AwaitApprovalBookmark, Indice.Features.Cases.AspNetCore';

UPDATE [Elsa].[Bookmarks]
SET ModelType = REPLACE(ModelType,
    'Indice.Features.Cases.Workflows.Bookmarks.AwaitAssignment.AwaitAssignmentBookmark, Indice.Features.Cases.AspNetCore',
    'Indice.Features.Cases.Workflows.Bookmarks.AwaitAssignmentBookmark, Indice.Features.Cases.Workflows')
WHERE ModelType = 'Indice.Features.Cases.Workflows.Bookmarks.AwaitAssignment.AwaitAssignmentBookmark, Indice.Features.Cases.AspNetCore';

UPDATE [Elsa].[Bookmarks]
SET ModelType = REPLACE(ModelType,
    'Indice.Features.Cases.Workflows.Bookmarks.AwaitEdit.AwaitEditBookmark, Indice.Features.Cases.AspNetCore',
    'Indice.Features.Cases.Workflows.Bookmarks.AwaitEditBookmark, Indice.Features.Cases.Workflows')
WHERE ModelType = 'Indice.Features.Cases.Workflows.Bookmarks.AwaitEdit.AwaitEditBookmark, Indice.Features.Cases.AspNetCore';

UPDATE [Elsa].[Bookmarks]
SET ModelType = REPLACE(ModelType,
    'Indice.Features.Cases.Workflows.Bookmarks.AwaitAction.AwaitActionBookmark, Indice.Features.Cases.AspNetCore',
    'Indice.Features.Cases.Workflows.Bookmarks.AwaitActionBookmark, Indice.Features.Cases.Workflows')
WHERE ModelType = 'Indice.Features.Cases.Workflows.Bookmarks.AwaitAction.AwaitActionBookmark, Indice.Features.Cases.AspNetCore';

-- Indice.Features.Cases.Workflows namespace - previous rc version
UPDATE [Elsa].[Bookmarks]
SET ModelType = REPLACE(ModelType,
    'Indice.Features.Cases.Workflows.Bookmarks.AwaitApproval.AwaitApprovalBookmark, Indice.Features.Cases.Workflows',
    'Indice.Features.Cases.Workflows.Bookmarks.AwaitApprovalBookmark, Indice.Features.Cases.Workflows')
WHERE ModelType = 'Indice.Features.Cases.Workflows.Bookmarks.AwaitApproval.AwaitApprovalBookmark, Indice.Features.Cases.Workflows';

UPDATE [Elsa].[Bookmarks]
SET ModelType = REPLACE(ModelType,
    'Indice.Features.Cases.Workflows.Bookmarks.AwaitAssignment.AwaitAssignmentBookmark, Indice.Features.Cases.Workflows',
    'Indice.Features.Cases.Workflows.Bookmarks.AwaitAssignmentBookmark, Indice.Features.Cases.Workflows')
WHERE ModelType = 'Indice.Features.Cases.Workflows.Bookmarks.AwaitAssignment.AwaitAssignmentBookmark, Indice.Features.Cases.Workflows';

UPDATE [Elsa].[Bookmarks]
SET ModelType = REPLACE(ModelType,
    'Indice.Features.Cases.Workflows.Bookmarks.AwaitEdit.AwaitEditBookmark, Indice.Features.Cases.Workflows',
    'Indice.Features.Cases.Workflows.Bookmarks.AwaitEditBookmark, Indice.Features.Cases.Workflows')
WHERE ModelType = 'Indice.Features.Cases.Workflows.Bookmarks.AwaitEdit.AwaitEditBookmark, Indice.Features.Cases.Workflows';

UPDATE [Elsa].[Bookmarks]
SET ModelType = REPLACE(ModelType,
    'Indice.Features.Cases.Workflows.Bookmarks.AwaitAction.AwaitActionBookmark, Indice.Features.Cases.Workflows',
    'Indice.Features.Cases.Workflows.Bookmarks.AwaitActionBookmark, Indice.Features.Cases.Workflows')
WHERE ModelType = 'Indice.Features.Cases.Workflows.Bookmarks.AwaitAction.AwaitActionBookmark, Indice.Features.Cases.Workflows';

-- Indice.AspNetCore.Features.Cases namespace - old
UPDATE [Elsa].[Bookmarks]
SET ModelType = REPLACE(ModelType,
    'Indice.AspNetCore.Features.Cases.Workflows.Bookmarks.AwaitApproval.AwaitApprovalBookmark, Indice.AspNetCore.Features.Cases',
    'Indice.Features.Cases.Workflows.Bookmarks.AwaitApprovalBookmark, Indice.Features.Cases.Workflows')
WHERE ModelType = 'Indice.AspNetCore.Features.Cases.Workflows.Bookmarks.AwaitApproval.AwaitApprovalBookmark, Indice.AspNetCore.Features.Cases';

UPDATE [Elsa].[Bookmarks]
SET ModelType = REPLACE(ModelType,
    'Indice.AspNetCore.Features.Cases.Workflows.Bookmarks.AwaitAssignment.AwaitAssignmentBookmark, Indice.AspNetCore.Features.Cases',
    'Indice.Features.Cases.Workflows.Bookmarks.AwaitAssignmentBookmark, Indice.Features.Cases.Workflows')
WHERE ModelType = 'Indice.AspNetCore.Features.Cases.Workflows.Bookmarks.AwaitAssignment.AwaitAssignmentBookmark, Indice.AspNetCore.Features.Cases';

UPDATE [Elsa].[Bookmarks]
SET ModelType = REPLACE(ModelType,
    'Indice.AspNetCore.Features.Cases.Workflows.Bookmarks.AwaitEdit.AwaitEditBookmark, Indice.AspNetCore.Features.Cases',
    'Indice.Features.Cases.Workflows.Bookmarks.AwaitEditBookmark, Indice.Features.Cases.Workflows')
WHERE ModelType = 'Indice.AspNetCore.Features.Cases.Workflows.Bookmarks.AwaitEdit.AwaitEditBookmark, Indice.AspNetCore.Features.Cases';

UPDATE [Elsa].[Bookmarks]
SET ModelType = REPLACE(ModelType,
    'Indice.AspNetCore.Features.Cases.Workflows.Bookmarks.AwaitAction.AwaitActionBookmark, Indice.AspNetCore.Features.Cases',
    'Indice.Features.Cases.Workflows.Bookmarks.AwaitActionBookmark, Indice.Features.Cases.Workflows')
WHERE ModelType = 'Indice.AspNetCore.Features.Cases.Workflows.Bookmarks.AwaitAction.AwaitActionBookmark, Indice.AspNetCore.Features.Cases';

-- Indice.Features.Cases namespace - old
UPDATE [Elsa].[Bookmarks]
SET ModelType = REPLACE(ModelType,
    'Indice.Features.Cases.Workflows.Bookmarks.AwaitApproval.AwaitApprovalBookmark, Indice.Features.Cases',
    'Indice.Features.Cases.Workflows.Bookmarks.AwaitApprovalBookmark, Indice.Features.Cases.Workflows')
WHERE ModelType = 'Indice.Features.Cases.Workflows.Bookmarks.AwaitApproval.AwaitApprovalBookmark, Indice.Features.Cases';

UPDATE [Elsa].[Bookmarks]
SET ModelType = REPLACE(ModelType,
    'Indice.Features.Cases.Workflows.Bookmarks.AwaitAssignment.AwaitAssignmentBookmark, Indice.Features.Cases',
    'Indice.Features.Cases.Workflows.Bookmarks.AwaitAssignmentBookmark, Indice.Features.Cases.Workflows')
WHERE ModelType = 'Indice.Features.Cases.Workflows.Bookmarks.AwaitAssignment.AwaitAssignmentBookmark, Indice.Features.Cases';

UPDATE [Elsa].[Bookmarks]
SET ModelType = REPLACE(ModelType,
    'Indice.Features.Cases.Workflows.Bookmarks.AwaitEdit.AwaitEditBookmark, Indice.Features.Cases',
    'Indice.Features.Cases.Workflows.Bookmarks.AwaitEditBookmark, Indice.Features.Cases.Workflows')
WHERE ModelType = 'Indice.Features.Cases.Workflows.Bookmarks.AwaitEdit.AwaitEditBookmark, Indice.Features.Cases';

UPDATE [Elsa].[Bookmarks]
SET ModelType = REPLACE(ModelType,
    'Indice.Features.Cases.Workflows.Bookmarks.AwaitAction.AwaitActionBookmark, Indice.Features.Cases',
    'Indice.Features.Cases.Workflows.Bookmarks.AwaitActionBookmark, Indice.Features.Cases.Workflows')
WHERE ModelType = 'Indice.Features.Cases.Workflows.Bookmarks.AwaitAction.AwaitActionBookmark, Indice.Features.Cases';
```
TODO for migrations: 
1. Check missing member handling behaviour for `WorkflowActor` and `AuditMeta` in `AssignCaseToUserActivity`.
2. `Indice.Features.Cases.Models.AwaitAssignmentInvokerInput` unused activity input data is probably ok not being migrated.

`WorkflowExecutionLogRecords` does not seem to be affected, ran a regex query to assert.

## Sample `Bookmarks` migration script
```csharp
using Elsa.Activities.Http;
using Elsa.Models;
using Elsa.Services;
using Indice.Features.Cases.Workflows.Activities;
using Indice.Features.Cases.Workflows.Bookmarks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

var executing = args.Contains("--execute");
var verbose = args.Contains("--verbose");
var connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=Indice.Cases.Workflow4;Trusted_Connection=True;MultipleActiveResultSets=true";

var builder = Host.CreateApplicationBuilder();
builder.AddCasesWorkflow(o => { o.ConfigureDbContext = (_, ef) => { ef.UseSqlServer(connectionString, sqlOptions => { sqlOptions.EnableRetryOnFailure(); }); }; });
builder.Services.AddLogging(b => { b.AddConsole(); });

var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var hasher = host.Services.GetRequiredService<IBookmarkHasher>();

var changed = 0;
var total = 0;
var batchSize = 500;
var offset = 0;
var approvals = 0;
var assignments = 0;
var edits = 0;
var actions = 0;
var http = 0;

await using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();

logger.LogInformation($"Started Processing Bookmarks. Batch Size: {batchSize}");
while (true) {
    if (verbose) {
        logger.LogInformation("Processing {from} to {to}", offset, offset + batchSize);
    }
    var bookmarks = await FetchDataBatchAsync(offset, batchSize);
    total += bookmarks.Count;
    if (bookmarks.Count == 0) {
        break;
    }

    await UpdateHashesAsync(bookmarks, hasher);

    offset += batchSize;
}

logger.LogInformation($"Finished Processing of {total} bookmarks");
logger.LogInformation($"Changed: {changed}");
logger.LogInformation($"Assignemts: {assignments}");
logger.LogInformation($"Approvals: {approvals}");
logger.LogInformation($"Edits: {edits}");
logger.LogInformation($"Actions: {actions}");
logger.LogInformation($"Http: {http}");


// Fetch a batch of data from the database
async Task<List<Bookmark>> FetchDataBatchAsync(int off, int bS) {
    var data = new List<Bookmark>();

    var query = $@"
            SELECT [Id], [TenantId], [Hash], [Model], [ModelType], [ActivityType], [ActivityId], [WorkflowInstanceId], [CorrelationId]
            FROM [Indice.Cases.Workflow4].[Elsa].[Bookmarks]
            ORDER BY [Id]
            OFFSET {off} ROWS FETCH NEXT {bS} ROWS ONLY;
        ";

    await using var command = new SqlCommand(query, connection);
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync()) {
        data.Add(new Bookmark {
            Id = reader.GetString(0),
            Hash = reader.GetString(2),
            Model = reader.GetString(3),
            ActivityType = reader.GetString(5),
            CorrelationId = reader.GetString(8)
        });
    }

    return data;
}

async Task UpdateHashesAsync(List<Bookmark> bookmarks, IBookmarkHasher hasher) {
    foreach (var bookmark in bookmarks) {
        if (bookmark.ActivityType is nameof(HttpEndpoint) or nameof(HttpEndpointWithValidation)) {
            http++;
            if (verbose) {
                logger.LogInformation($"Encountered Activity Type: {bookmark.ActivityType} with Correlation Id: {bookmark.CorrelationId}");
            }

            continue;
        }

        string updateQuery = @"
                UPDATE [Indice.Cases.Workflow4].[Elsa].[Bookmarks]
                SET [Hash] = @Hash
                WHERE [Id] = @Id;
            ";

        var newHash = hasher.Hash(MapToBookmark(bookmark));
        if (bookmark.Hash != newHash && verbose) {
            logger.LogInformation($"Hash for bookmark {bookmark.Id}, caseId {bookmark.CorrelationId} and type {bookmark.ActivityType} will be changed to {newHash}. Model: {bookmark.Model}");
        }
        
        changed++;

        if (!executing) {
            continue;
        }

        await using var command = new SqlCommand(updateQuery, connection);
        command.Parameters.AddWithValue("@Hash", newHash);
        command.Parameters.AddWithValue("@Id", bookmark.Id);
        await command.ExecuteNonQueryAsync();
    }
}

IBookmark MapToBookmark(Bookmark bookmark) {
    if (bookmark.ActivityType == nameof(AwaitEditActivity)) {
        edits++;
        return new AwaitEditBookmark(bookmark.CorrelationId);
    }

    if (bookmark.ActivityType == nameof(AwaitActionActivity)) {
        actions++;
        return JsonConvert.DeserializeObject<AwaitActionBookmark>(bookmark.Model) ?? throw new InvalidOperationException($"Could not deserialize bookmark with Id: {bookmark.Id}.");
    }

    if (bookmark.ActivityType == nameof(AwaitAssignmentActivity)) {
        assignments++;
        return new AwaitAssignmentBookmark(bookmark.CorrelationId);
    }

    if (bookmark.ActivityType == nameof(AwaitApprovalActivity)) {
        approvals++;
        return new AwaitApprovalBookmark(bookmark.CorrelationId);
    }

    throw new NotSupportedException($"Unknown activity type {bookmark}");
}
```
