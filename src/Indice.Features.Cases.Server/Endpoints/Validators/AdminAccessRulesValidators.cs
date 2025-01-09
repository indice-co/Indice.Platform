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

        // at least on of the following should be provided
        RuleFor(x => x.MemberRole).NotEmpty().When(x => string.IsNullOrEmpty(x.MemberGroupId) && string.IsNullOrEmpty(x.MemberUserId));
        RuleFor(x => x.MemberGroupId).NotEmpty().When(x => string.IsNullOrEmpty(x.MemberRole) && string.IsNullOrEmpty(x.MemberUserId));
        RuleFor(x => x.MemberUserId).NotEmpty().When(x => string.IsNullOrEmpty(x.MemberRole) && string.IsNullOrEmpty(x.MemberGroupId));
        RuleFor(x => x.MemberUserId).Must(MembersAreValid).WithMessage("Only one of the following properties must be provided: MemberRole, MemberGroupId, MemberUserId");

        RuleFor(x => x.RuleCaseTypeId).NotNull().When(x => !x.RuleCheckpointTypeId.HasValue && !x.RuleCaseId.HasValue);
        RuleFor(x => x.RuleCheckpointTypeId).NotNull().When(x => !x.RuleCaseTypeId.HasValue && !x.RuleCaseId.HasValue);
        RuleFor(x => x.RuleCaseId).NotNull().When(x => !x.RuleCaseTypeId.HasValue && !x.RuleCheckpointTypeId.HasValue);
        RuleFor(x => x.RuleCaseId).Must(RulesAreValid).WithMessage("At least one resource rule must be set (RuleCaseId, RuleCheckpointTypeId, RuleCaseTypeId or RuleCaseId & RuleCheckpointTypeId)");
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
    /// Validates the request, so that at least one of MemberRole, MemberGroupId, MemberUserId is specified. Whitespaces are allowed.
    public AddCaseAccessRuleRequestValidator() {
        RuleFor(x => x.MemberRole).NotEmpty().When(x => string.IsNullOrEmpty(x.MemberGroupId) && string.IsNullOrEmpty(x.MemberUserId));
        RuleFor(x => x.MemberGroupId).NotEmpty().When(x => string.IsNullOrEmpty(x.MemberRole) && string.IsNullOrEmpty(x.MemberUserId));
        RuleFor(x => x.MemberUserId).NotEmpty().When(x => string.IsNullOrEmpty(x.MemberRole) && string.IsNullOrEmpty(x.MemberGroupId));
        RuleFor(x => x.MemberUserId).Must(MembersAreValid).WithMessage("Only one of the following properties must be provided: MemberRole, MemberGroupId, MemberUserId");
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