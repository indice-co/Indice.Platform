using System.Text.Json.Nodes;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Models;
using Indice.Security;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Core.Data;

/// <summary>Cases DbContext intialization extensions</summary>
public static class CasesDbInitalizerExtesnions
{
    /// <summary>
    /// Create database if not exists and seed with initial data
    /// </summary>
    /// <param name="dbContext">The database context</param>
    /// <param name="options">Seed options</param>
    /// <returns>The Task</returns>
    public async static Task InitializeAsync(this CasesDbContext dbContext, IOptions<CasesDbIntialDataOptions> options) {
        if (await dbContext.Database.EnsureCreatedAsync()) {
            await dbContext.SeedAsync(options);
        }
    }

    /// <summary>
    /// Seed the database to its initial state
    /// </summary>
    /// <param name="dbContext">The database context</param>
    /// <param name="options">Seed options</param>
    /// <returns></returns>
    public async static Task SeedAsync(this CasesDbContext dbContext, IOptions<CasesDbIntialDataOptions> options) {
        var data = options.Value;

        // add defaults...
        await dbContext.AddSampleCasesAsync();

        // add user defined...
        dbContext.CaseTypes.AddRange(data.CaseTypes);
        if (!data.CaseTypes.All(x => x.CheckpointTypes.Count != 0)) {
            // add default checkpoint types if not assigned
            data.CaseTypes.ForEach(ct => dbContext.CheckpointTypes.AddRange(GetDefaultCheckPointTypes(ct.Id)));
        }
        dbContext.Cases.AddRange(data.Cases);
        await dbContext.SaveChangesAsync();
    }
    private async static Task AddSampleCasesAsync(this CasesDbContext dbContext) {
        var sampleCaseType = GetSampleCaseTypes().First();
        var createdBy = new AuditMeta() {
            Id = "seed",
            Email = "system",
            Name = "system",
            When = DateTimeOffset.UtcNow,
        };
        dbContext.CaseTypes.AddRange([sampleCaseType]);

        var sampleCase = new DbCase() {
            Id = Guid.Parse("02723481-0915-4fc3-bb21-123f2e704ca3"),
            CaseTypeId = sampleCaseType.Id,
            CreatedBy = createdBy,
            Draft = false,
            Channel = "seed",
            ReferenceNumber = 1,
            Owner = new ContactMeta {
                FirstName = "John",
                LastName = "Doe",
                UserId = Guid.NewGuid().ToString(),
                Reference = "0000000",
            }
        };
        var sampleCaseData = new DbCaseData() {
            CreatedBy = createdBy.Clone(),
            CaseId = sampleCase.Id,
            Data = JsonNode.Parse("""
    {
        "postOfficeBox": "123",
        "streetAddress": "456 Main St",
        "locality": "Cityville",
        "region": "State",
        "postalCode": "12345",
        "countryName": "Country"
    }
""")!,
        };
        var submittedCheckPoint = new DbCheckpoint {
            Id = Guid.Parse("c47f341d-ce04-40ab-b784-96eba5f79e8e"),
            CheckpointTypeId = sampleCaseType.CheckpointTypes.First().Id,
            CaseId = sampleCase.Id,
            CreatedBy = createdBy.Clone(),
        };
        dbContext.Cases.Add(sampleCase);
        dbContext.CaseData.Add(sampleCaseData);
        dbContext.Checkpoints.Add(submittedCheckPoint);
        await dbContext.SaveChangesAsync();

        sampleCase.CheckpointId = submittedCheckPoint.Id;
        sampleCase.PublicCheckpointId = submittedCheckPoint.Id;
        sampleCase.DataId = sampleCaseData.Id;
        await dbContext.SaveChangesAsync();
    }

    private static List<DbCaseType> GetSampleCaseTypes() => [
        new() {
            Id = Guid.Parse("8a1f687c-41f9-4442-9d94-7118e1af0226"),
            Code = "SampleAddress", Description = "Sample Address", Title ="Sample Address",
            CanCreateRoles = string.Join(',' , [BasicRoleNames.Administrator]),
            CheckpointTypes = GetDefaultCheckPointTypes(Guid.Parse("8a1f687c-41f9-4442-9d94-7118e1af0226")),
            DataSchema = JsonNode.Parse(AddressSampleSchema.Trim()),
            Layout = JsonNode.Parse("""
                [
                  { "key": "postalCode", "title": "Post code", "type": "text", "placeholder": "Post code" },
                  { "key": "postOfficeBox", "title": "PO Box", "type": "text", "placeholder": "PO Box" },
                  { "key": "streetAddress", "title": "Street", "type": "text", "placeholder": "Address Line 1" },
                  { "key": "extendedAddress", "title": "Line 2", "type": "text", "placeholder": "Line 2" },
                  { "key": "locality", "title": "Municipality", "type": "text", "placeholder": "Municipality" },
                  { "key": "region", "title": "Region", "type": "text", "placeholder": "Region" },
                  { "key": "countryName", "title": "Country", "type": "text", "placeholder": "Country" }
                ]
                """),
            Tags = "tagOne,tagTwo"
        },
    ];

    private static List<DbCheckpointType> GetDefaultCheckPointTypes(Guid caseTypeId) => [
        new() { Code = nameof(CaseStatus.Submitted),Title = nameof(CaseStatus.Submitted), Private = false, Status = CaseStatus.Submitted, CaseTypeId = caseTypeId },
    ];


    private const string AddressSampleSchema = """
    {
        "type": "object",
        "properties": {
            "postOfficeBox": {
                "type": "string"
            },
            "extendedAddress": {
                "type": "string"
            },
            "streetAddress": {
                "type": "string"
            },
            "locality": {
                "type": "string"
            },
            "region": {
                "type": "string"
            },
            "postalCode": {
                "type": "string"
            },
            "countryName": {
                "type": "string"
            }
        },
        "required": ["locality", "region", "countryName"],
        "dependentRequired": {
            "postOfficeBox": ["streetAddress"],
            "extendedAddress": ["streetAddress"]
        } 
    }
""";
}
