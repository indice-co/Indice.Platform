
namespace Indice.Features.Cases.Core.Models.Requests;

/// <summary>Request to replace in case rules a user with another one</summary>
public class ReplaceCaseAccessRuleUserRequest
{
    /// <summary>
    /// The id of the user that is already assinged to the rules
    /// </summary>
    public required string ExistingUserId { get; set; }
    /// <summary>
    /// The id of the user that will replace the user in the rules
    /// </summary>
    public required string ReplacementUserId { get; set; }
}