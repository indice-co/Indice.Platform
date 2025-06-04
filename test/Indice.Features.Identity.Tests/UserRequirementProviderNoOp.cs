using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Identity.Tests;

internal class UserRequirementProviderNoOp : IUserRequirementProvider<User>
{
    Task<UserValidationRequirement> IUserRequirementProvider<User>.GetNextAsync(HttpContext httpContext, User user) {
        return Task.FromResult(UserValidationRequirement.None);
    }
}
