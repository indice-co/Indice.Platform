using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Identity.Tests;

internal class UserActivityProviderNoOp : IUserActivityProvider<User>
{
    public Task<UserActivityRequirement> GetNextAsync(HttpContext httpContext, User user) {
        throw new NotImplementedException();
    }
}
