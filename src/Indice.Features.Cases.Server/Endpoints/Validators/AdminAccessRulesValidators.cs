using FluentValidation;
using Indice.Features.Cases.Core.Models.Requests;

namespace Indice.Features.Cases.Server.Endpoints.Validators;

/// <summary>
/// Validator for Add AccessRule Request 
/// </summary>
public class AddAccessRuleRequestValidator : AbstractValidator<AddAccessRuleRequest>
{
    /// <inheritdoc/>
    public AddAccessRuleRequestValidator() {

        RuleFor(x => x.MemberUserId).Must(MembersAreValid).WithMessage("One member property must be set. Either MemberRole or MemberGroupId or MemberUserId");
        RuleFor(x => x.RuleCaseId).Must(RulesAreValid).WithMessage("One resource rule must be set. Either set RuleCaseId or RuleCheckpointTypeId or RuleCaseTypeId or RuleCaseId & RuleCheckpointTypeId");
    }

    private static bool MembersAreValid(AddAccessRuleRequest rule, string? prop) {
        // "obj" is the important parameter here - it's the class instance.
        // not going to use "prop" parameter.
        return new[] { !string.IsNullOrEmpty(rule.MemberRole), !string.IsNullOrEmpty(rule.MemberGroupId), !string.IsNullOrEmpty(rule.MemberUserId) }
                               .Count(x => x) == 1;
    }

    private static bool RulesAreValid(AddAccessRuleRequest rule, Guid? prop) {
        var ruleValidation = new[] { rule.RuleCaseTypeId.HasValue, rule.RuleCheckpointTypeId.HasValue, rule.RuleCaseId.HasValue }
                              .Count(x => x) == 1;
        var ruleValidationCaseCheckpoint = rule.RuleCheckpointTypeId.HasValue && rule.RuleCaseId.HasValue && !rule.RuleCaseTypeId.HasValue;

        return ruleValidation || ruleValidationCaseCheckpoint;
    }
}

/// <summary>
/// Validator for Batch AddAccessRuleRequest
/// </summary>
public class BatchAccessRuleRequestValidator : AbstractValidator<List<AddAccessRuleRequest>>
{
    /// <inheritdoc/>
    public BatchAccessRuleRequestValidator() {
        RuleForEach(x => x).SetValidator(new AddAccessRuleRequestValidator());
    }
}


/// <summary>
/// Validator for Add CaseAccessRuleRequest
/// </summary>
public class AddCaseAccessRuleRequestValidator : AbstractValidator<AddCaseAccessRuleRequest>
{
    /// <summary>
    /// Validates the request, so that at least one of MemberRole, MemberGroupId, MemberUserId is specified. Whitespaces are allowed.
    /// </summary>
    public AddCaseAccessRuleRequestValidator() {
        RuleFor(x => x.MemberUserId).Must(MembersAreValid).WithMessage("One member property must be set. Either MemberRole or MemberGroupId or MemberUserId");
    }
    private static bool MembersAreValid(AddCaseAccessRuleRequest rule, string? prop) {
        // "obj" is the important parameter here - it's the class instance.
        // not going to use "prop" parameter.
        return new[] { !string.IsNullOrEmpty(rule.MemberRole), !string.IsNullOrEmpty(rule.MemberGroupId), !string.IsNullOrEmpty(rule.MemberUserId) }
                               .Count(x => x) == 1;
    }
}

/// <summary>
/// Validator for Batch AddCaseAccessRuleRequest
/// </summary>
public class BatchAddCaseAccessRuleRequestValidator : AbstractValidator<List<AddCaseAccessRuleRequest>>
{
    /// <inheritdoc/>
    public BatchAddCaseAccessRuleRequestValidator() {
        RuleForEach(x => x).SetValidator(new AddCaseAccessRuleRequestValidator());
    }
}