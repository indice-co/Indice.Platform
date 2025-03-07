using System.Text;
using System.Text.Json;
using Indice.Extensions.Configuration.Database.Data.Models;
using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data;
using Indice.Features.Risk.Core.Models;
using Indice.Serialization;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Indice.Features.Risk.Core.Stores;
internal class RuleStoreEntityFrameworkCore : IRuleOptionsStore
{
    private readonly RiskDbContext _context;

    public RuleStoreEntityFrameworkCore(RiskDbContext context) {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Dictionary<string, string?>> GetRuleOptions(string ruleName) {
        if (string.IsNullOrWhiteSpace(ruleName)) {
            return new Dictionary<string, string?>();
        }
        var results = await _context.AppSettings
            .AsNoTracking()
            .Where(x => x.Key.StartsWith($"{Constants.RuleOptionsSectionName}:{ruleName}:"))
            .ToDictionaryAsync(
                x => x.Key.Replace($"{Constants.RuleOptionsSectionName}:{ruleName}:", string.Empty),
                x => x.Value
        );
        results = TransformEligibleEvents(results);
        return results;
    }

    public async Task UpdateRuleOptions(string ruleName, RuleOptions ruleOptions) {
        if (string.IsNullOrWhiteSpace(ruleName)) {
            return;
        }

        var serializerOptions = JsonSerializerOptionDefaults.GetDefaultSettings();
        serializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString;
        var jsonString = JsonSerializer.Serialize(ruleOptions, ruleOptions.GetType(), serializerOptions);

        // Creating an IConfiguration instance and enumerating it is faster and safe.
        // It avoids the need to map and name the JSON properties to appropriate key/value pairs.
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonStream(stream);
            var configuration = configurationBuilder.Build();
            var settings = configuration.AsEnumerable().Where(x => x.Value is not null);
            foreach (var item in settings) {
                var key = $"{Constants.RuleOptionsSectionName}:{ruleName}:{item.Key}";
                var existingItem = await _context.AppSettings.FirstOrDefaultAsync(x => x.Key.ToLower() == key.ToLower());
                if (existingItem is not null) {
                    existingItem.Value = item.Value;
                } else {
                    var newItem = new DbAppSetting {
                        Key = key,
                        Value = item.Value
                    };
                    await _context.AppSettings.AddAsync(newItem);
                }
            }
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Transforms eligible events to a comma-separated list
    /// </summary>
    private Dictionary<string, string?> TransformEligibleEvents(Dictionary<string, string?> dictionary) {
        var keysToMerge = dictionary.Keys.Where(key => key.StartsWith(nameof(RuleOptions.EligibleEvents))).ToList();
        var eligibleEvents = string.Join(",", keysToMerge.Select(key => dictionary[key]));
        dictionary["eligibleEvents"] = eligibleEvents;
        foreach (var key in keysToMerge) {
            dictionary.Remove(key);
        }
        return dictionary;
    }
}
