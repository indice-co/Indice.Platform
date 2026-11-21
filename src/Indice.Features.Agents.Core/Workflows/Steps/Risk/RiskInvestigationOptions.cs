using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps.Risk;

/// <summary>Settings of the risk investigation agent, bound from the <c>Dex:Risk</c> configuration section.</summary>
public class RiskInvestigationOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Dex:Risk";

    /// <summary>
    /// Roles of which a caller needs one to use the agent. There is deliberately no default: the agent builds evidence about
    /// bank customers, so a host must decide who may use it. Startup fails when this is empty.
    /// </summary>
    public List<string> RequiredRoles { get; set; } = [];

    /// <summary>The knowledge base category holding the antifraud knowledge. It should also be a restricted category (<c>Dex:Taxonomy:RestrictedCategories</c>).</summary>
    public string KnowledgeCategory { get; set; } = "antifraud-kb";

    /// <summary>
    /// What a transaction number may look like. A value that does not match is never sent to the database: it is asked for
    /// again. The number must also appear literally in what the user wrote, so a model cannot invent one.
    /// </summary>
    public string TunPattern { get; set; } = @"^[A-Za-z0-9._\-]{3,64}$";

    /// <summary>How many times the agent asks for missing or unmatched details before it gives up.</summary>
    public int MaxInputAttempts { get; set; } = 3;

    /// <summary>
    /// The width, in days, of the successive windows searched for the transaction around the date the user gave. The first is
    /// centred on that day; a wider one is tried only when the narrower finds nothing. The risk MCP server caps this window.
    /// </summary>
    public List<int> AnchorSearchWindowDays { get; set; } = [1, 7];

    /// <summary>The language of the report when the user's own language is not Greek or English.</summary>
    public string DefaultLanguage { get; set; } = "el";

    /// <summary>The time zone dates are read and reported in, next to UTC. The databases store UTC.</summary>
    public string TimeZoneId { get; set; } = "Europe/Athens";

    /// <summary>Partially masks phone numbers and e-mail addresses in the report. The unmasked values stay in the database rows the report cites.</summary>
    public bool MaskContactDetails { get; set; } = true;

    /// <summary>How many days before the transaction the credential and profile changes are looked up.</summary>
    public int PriorChangesDays { get; set; } = 30;

    /// <summary>How many days before the transaction the customer's usual IPs, countries and devices are read from.</summary>
    public int UsualPatternDays { get; set; } = 90;

    /// <summary>The most step events (second factor sent, third factor, final) whose score breakdown is fetched.</summary>
    public int MaxStepEvents { get; set; } = 10;

    /// <summary>The Athens-style time zone, resolved. Falls back to UTC when the id is unknown on this machine.</summary>
    internal TimeZoneInfo ResolveTimeZone() {
        try {
            return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        } catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException) {
            return TimeZoneInfo.Utc;
        }
    }
}

/// <summary>Startup validation of <see cref="RiskInvestigationOptions"/>.</summary>
public sealed class RiskInvestigationOptionsValidator : IValidateOptions<RiskInvestigationOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, RiskInvestigationOptions options) {
        var failures = new List<string>();
        if (options.RequiredRoles.Count == 0 || options.RequiredRoles.Any(string.IsNullOrWhiteSpace)) {
            failures.Add($"{RiskInvestigationOptions.SectionName}:RequiredRoles must list at least one role. The risk investigation agent exposes evidence about bank customers and is never open to everyone.");
        }
        if (string.IsNullOrWhiteSpace(options.KnowledgeCategory)) {
            failures.Add($"{RiskInvestigationOptions.SectionName}:KnowledgeCategory must be configured.");
        }
        try {
            _ = new Regex(options.TunPattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        } catch (ArgumentException) {
            failures.Add($"{RiskInvestigationOptions.SectionName}:TunPattern is not a valid regular expression.");
        }
        if (options.MaxInputAttempts < 1) {
            failures.Add($"{RiskInvestigationOptions.SectionName}:MaxInputAttempts must be at least 1.");
        }
        if (options.AnchorSearchWindowDays.Count == 0 || options.AnchorSearchWindowDays.Any(days => days < 1) || !options.AnchorSearchWindowDays.SequenceEqual(options.AnchorSearchWindowDays.Order())) {
            failures.Add($"{RiskInvestigationOptions.SectionName}:AnchorSearchWindowDays must be positive, ascending, and not empty.");
        }
        if (options.PriorChangesDays < 1 || options.UsualPatternDays < 1 || options.MaxStepEvents < 1) {
            failures.Add($"{RiskInvestigationOptions.SectionName}: PriorChangesDays, UsualPatternDays and MaxStepEvents must be positive.");
        }
        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}
