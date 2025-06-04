using Indice.Features.Cases.Core.Models.Requests;
using Indice.Features.Cases.Server.Endpoints.Validators;

namespace Indice.Features.Messages.Tests;

public class AddAccessRuleRequestValidationTests
{
    //AddAccessRuleRequest
    [Fact]
    public void Valid_AddAccessRuleRequest_Case_Member() {
        AddAccessRuleRequest request = new AddAccessRuleRequest() {
            RuleCaseId = Guid.NewGuid(),
            MemberUserId = "MockUser",
        };
        AddAccessRuleRequestValidator validator = new AddAccessRuleRequestValidator();
        Assert.True(validator.Validate(request).IsValid);
    }
    [Fact]
    public void Valid_AddAccessRuleRequest_Case_Role() {
        AddAccessRuleRequest request = new AddAccessRuleRequest() {
            RuleCaseId = Guid.NewGuid(),
            MemberRole = "Admin",
        };
        AddAccessRuleRequestValidator validator = new AddAccessRuleRequestValidator();
        Assert.True(validator.Validate(request).IsValid);
    }
    [Fact]
    public void Valid_AddAccessRuleRequest_Case_Group() {
        AddAccessRuleRequest request = new AddAccessRuleRequest() {
            RuleCaseId = Guid.NewGuid(),
            MemberGroupId = "Group"
        };
        AddAccessRuleRequestValidator validator = new AddAccessRuleRequestValidator();
        Assert.True(validator.Validate(request).IsValid);
    }

    [Fact]
    public void Valid_AddAccessRuleRequest_CheckPoint_Member() {
        AddAccessRuleRequest request = new AddAccessRuleRequest() {
            RuleCheckpointTypeId = Guid.NewGuid(),
            MemberUserId = "MockUser"
        };
        AddAccessRuleRequestValidator validator = new AddAccessRuleRequestValidator();
        Assert.True(validator.Validate(request).IsValid);
    }
    [Fact]
    public void Valid_AddAccessRuleRequest_CheckPoint_Role() {
        AddAccessRuleRequest request = new AddAccessRuleRequest() {
            RuleCheckpointTypeId = Guid.NewGuid(),
            MemberRole = "Admin"
        };
        AddAccessRuleRequestValidator validator = new AddAccessRuleRequestValidator();
        Assert.True(validator.Validate(request).IsValid);
    }
    [Fact]
    public void Valid_AddAccessRuleRequest_CheckPoint_Group() {
        AddAccessRuleRequest request = new AddAccessRuleRequest() {
            RuleCheckpointTypeId = Guid.NewGuid(),
            MemberGroupId = "Group"
        };
        AddAccessRuleRequestValidator validator = new AddAccessRuleRequestValidator();
        Assert.True(validator.Validate(request).IsValid);
    }

    [Fact]
    public void Valid_AddAccessRuleRequest_CaseType_Member() {
        AddAccessRuleRequest request = new AddAccessRuleRequest() {
            RuleCaseTypeId = Guid.NewGuid(),
            MemberUserId = "MockUser"
        };
        AddAccessRuleRequestValidator validator = new AddAccessRuleRequestValidator();
        Assert.True(validator.Validate(request).IsValid);
    }
    [Fact]
    public void Valid_AddAccessRuleRequest_CaseType_Role() {
        AddAccessRuleRequest request = new AddAccessRuleRequest() {
            RuleCaseTypeId = Guid.NewGuid(),
            MemberRole = "Admin"
        };
        AddAccessRuleRequestValidator validator = new AddAccessRuleRequestValidator();
        Assert.True(validator.Validate(request).IsValid);
    }
    [Fact]
    public void Valid_AddAccessRuleRequest_CaseType_Group() {
        AddAccessRuleRequest request = new AddAccessRuleRequest() {
            RuleCaseTypeId = Guid.NewGuid(),
            MemberGroupId = "Group"
        };
        AddAccessRuleRequestValidator validator = new AddAccessRuleRequestValidator();
        Assert.True(validator.Validate(request).IsValid);
    }

    [Fact]
    public void Valid_AddAccessRuleRequest_Case_CheckPoint_Member() {
        AddAccessRuleRequest request = new AddAccessRuleRequest() {
            RuleCaseId = Guid.NewGuid(),
            RuleCheckpointTypeId = Guid.NewGuid(),
            MemberUserId = "MockUser"
        };

        AddAccessRuleRequestValidator validator = new AddAccessRuleRequestValidator();
        Assert.True(validator.Validate(request).IsValid);
    }
    [Fact]
    public void Valid_AddAccessRuleRequest_Case_CheckPoint_Role() {
        AddAccessRuleRequest request = new AddAccessRuleRequest() {
            RuleCaseId = Guid.NewGuid(),
            RuleCheckpointTypeId = Guid.NewGuid(),
            MemberRole = "Admin",

        };

        AddAccessRuleRequestValidator validator = new AddAccessRuleRequestValidator();
        Assert.True(validator.Validate(request).IsValid);
    }
    [Fact]
    public void Valid_AddAccessRuleRequest_Case_CheckPoint_Group() {
        AddAccessRuleRequest request = new AddAccessRuleRequest() {
            RuleCaseId = Guid.NewGuid(),
            RuleCheckpointTypeId = Guid.NewGuid(),
            MemberGroupId = "Group"

        };
        AddAccessRuleRequestValidator validator = new AddAccessRuleRequestValidator();
        Assert.True(validator.Validate(request).IsValid);
    }


    [Fact]
    public void Invalid_AddAccessRuleRequest_Rules_All() {
        AddAccessRuleRequest request = new AddAccessRuleRequest() {
            RuleCaseId = Guid.NewGuid(),
            RuleCheckpointTypeId = Guid.NewGuid(),
            RuleCaseTypeId = Guid.NewGuid(),
            MemberUserId = "MockUser",
            MemberRole = "member",
            MemberGroupId = "group"
        };
        AddAccessRuleRequestValidator validator = new AddAccessRuleRequestValidator();
        Assert.False(validator.Validate(request).IsValid);
    }
    [Fact]
    public void Invalid_AddAccessRuleRequest_Rules_Case_CaseType() {
        AddAccessRuleRequest request = new AddAccessRuleRequest() {
            RuleCaseId = Guid.NewGuid(),
            RuleCaseTypeId = Guid.NewGuid(),
            MemberUserId = "MockUser",
        };
        AddAccessRuleRequestValidator validator = new AddAccessRuleRequestValidator();
        Assert.False(validator.Validate(request).IsValid);
    }

    [Fact]
    public void Invalid_AddAccessRuleRequest_Rules_CaseType_Checkpoint() {
        AddAccessRuleRequest request = new AddAccessRuleRequest() {
            RuleCheckpointTypeId = Guid.NewGuid(),
            RuleCaseTypeId = Guid.NewGuid(),
            MemberUserId = "MockUser",
        };
        AddAccessRuleRequestValidator validator = new AddAccessRuleRequestValidator();
        Assert.False(validator.Validate(request).IsValid);
    }

    [Fact]
    public void Invalid_AddAccessRuleRequest_Members_User_Role() {
        AddAccessRuleRequest request = new AddAccessRuleRequest() {
            RuleCaseId = Guid.NewGuid(),
            MemberUserId = "MockUser",
            MemberRole = "Admin"
        };
        AddAccessRuleRequestValidator validator = new AddAccessRuleRequestValidator();
        Assert.False(validator.Validate(request).IsValid);
    }
    [Fact]
    public void Invalid_AddAccessRuleRequest_Members_User_Group() {
        AddAccessRuleRequest request = new AddAccessRuleRequest() {
            RuleCaseId = Guid.NewGuid(),
            MemberUserId = "MockUser",
            MemberGroupId = "Group"
        };
        AddAccessRuleRequestValidator validator = new AddAccessRuleRequestValidator();
        Assert.False(validator.Validate(request).IsValid);
    }
    [Fact]
    public void Invalid_AddAccessRuleRequest_Members_Role_Group() {
        AddAccessRuleRequest request = new AddAccessRuleRequest() {
            RuleCaseId = Guid.NewGuid(),
            MemberRole = "Admin",
            MemberGroupId = "Group"
        };
        AddAccessRuleRequestValidator validator = new AddAccessRuleRequestValidator();
        Assert.False(validator.Validate(request).IsValid);
    }
}