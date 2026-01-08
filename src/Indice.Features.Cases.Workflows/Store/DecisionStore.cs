using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using System.Text.Json;
using Indice.Features.Cases.Workflows.Models.Decision;
using Indice.Serialization;
using RulesEngine.Models;

namespace Indice.Features.Cases.Workflows.Store;

public class DecisionStore
{
    private static ConcurrentDictionary<string, Dictionary<string, Workflow>> _dictionary;
    private static readonly string BasePath = "DecisionStore";
    public async Task CreateDecision(string caseTypeCode, string decisionName) {
        var path = Path.Combine(BasePath, caseTypeCode);
        Directory.CreateDirectory(path);
        var decisionPath = Path.Combine(path, $"{decisionName}.json");
        var workflow = new Workflow {
            WorkflowName = decisionName,
            Rules = Array.Empty<Rule>()
        };
        var json = JsonSerializer.Serialize(workflow, JsonSerializerOptionDefaults.GetDefaultSettings(JavaScriptEncoder.UnsafeRelaxedJsonEscaping));
        try {
            await using var stream = new FileStream(
                decisionPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None
            );

            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(json);
        } catch (IOException) { }
    }
    
    public async Task CreateRules(string caseTypeCode, DecisionTable decisionTable, Rule[] rules) {
        var decisionPath = Path.Combine(BasePath, caseTypeCode, $"{decisionTable.DecisionName}.json");

        var workflow = new Workflow {
            WorkflowName = decisionTable.DecisionName,
            Rules = rules
        };

        var json = JsonSerializer.Serialize(workflow, JsonSerializerOptionDefaults.GetDefaultSettings(JavaScriptEncoder.UnsafeRelaxedJsonEscaping));
        await File.WriteAllTextAsync(decisionPath, json);
        
        var decisionTablePath = Path.Combine(BasePath, caseTypeCode, $"{decisionTable.DecisionName}-decisionTable.json");
        await File.WriteAllTextAsync(decisionTablePath, JsonSerializer.Serialize(decisionTable, JsonSerializerOptionDefaults.GetDefaultSettings(JavaScriptEncoder.UnsafeRelaxedJsonEscaping)));
    }

    public async Task<DecisionTable?> GetDecisionTable(string caseTypeCode, string decisionName) {
        var decisionTablePath = Path.Combine(BasePath, caseTypeCode, $"{decisionName}-decisionTable.json");
        try {
            var json = await File.ReadAllTextAsync(decisionTablePath);
            return JsonSerializer.Deserialize<DecisionTable>(json, JsonSerializerOptionDefaults.GetDefaultSettings(JavaScriptEncoder.UnsafeRelaxedJsonEscaping))!;
        } catch (Exception ex) {
            return null;
        }
    }

    public async Task<Workflow> GetDecision(string caseTypeCode, string decisionName) {
        var decisionPath = Path.Combine(BasePath, caseTypeCode, $"{decisionName}.json");

        var json = await File.ReadAllTextAsync(decisionPath);
        return JsonSerializer.Deserialize<Workflow>(json, JsonSerializerOptionDefaults.GetDefaultSettings(JavaScriptEncoder.UnsafeRelaxedJsonEscaping))!;
        // return Task.FromResult(_dictionary[caseTypeCode][decisionName]);
    }
}