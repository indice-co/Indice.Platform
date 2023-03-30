using Bogus.DataSets;
using Humanizer;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Server.Manager.Models;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Identity.Server.Manager;
internal static class RoleHandlers
{

    internal static async Task<Ok<ResultSet<RoleInfo>>> GetRoles(RoleManager<Role> roleManager, ListOptions options) {
        var query = roleManager.Roles.AsNoTracking();
        if (!string.IsNullOrEmpty(options.Search)) {
            var searchTerm = options.Search.ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(searchTerm) || x.Description.Contains(searchTerm));
        }
        var roles = await query.Select(x => new RoleInfo {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description
        })
        .ToResultSetAsync(options);
        return TypedResults.Ok(roles);
    }
    internal static async Task<Results<Ok<RoleInfo>, NotFound>> GetRole(RoleManager<Role> roleManager, string roleId) {
        var role = await roleManager.FindByIdAsync(roleId);
        if (role == null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(new RoleInfo {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description
        });
    }
    internal static async Task<Results<CreatedAtRoute<RoleInfo>, ValidationProblem>> CreateRole(RoleManager<Role> roleManager, CreateRoleRequest request) {
        if (request is null) {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>() { [""] = new[] { "Request body cannot be null." } });
        }
        var exists = await roleManager.RoleExistsAsync(request.Name);
        if (exists) {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>() { [nameof(CreateRoleRequest.Name).Camelize()] = new[] { $"A claim type with name {request.Name} already exists." } });
        }
        var role = new Role {
            Id = $"{Guid.NewGuid()}",
            Name = request.Name,
            Description = request.Description
        };
        var result = await roleManager.CreateAsync(role);
        return TypedResults.CreatedAtRoute(new RoleInfo {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description
        }, nameof(GetRole), new { roleId = role.Id });
    }
    internal static async Task<Results<Ok<RoleInfo>, NotFound>> UpdateRole(RoleManager<Role> roleManager, string roleId, UpdateRoleRequest request) {
        var role = await roleManager.FindByIdAsync(roleId);
        if (role == null) {
            return TypedResults.NotFound();
        }
        role.Description = request.Description;
        await roleManager.UpdateAsync(role);
        return TypedResults.Ok(new RoleInfo {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description
        });
    }
    internal static async Task<Results<NoContent, NotFound>> DeleteRole(RoleManager<Role> roleManager, string roleId) {
        var role = await roleManager.FindByIdAsync(roleId);
        if (role == null) {
            return TypedResults.NotFound();
        }
        await roleManager.DeleteAsync(role);
        return TypedResults.NoContent();
    }
}
