using Humanizer;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Server.Manager.Models;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Identity.Server.Manager;
internal static class ClaimTypeHandlers
{

    internal static async Task<Ok<ResultSet<ClaimTypeInfo>>> GetClaimTypes(ExtendedConfigurationDbContext configurationDbContext, ListOptions options, ClaimTypesListFilter filter) {

        var query = configurationDbContext.ClaimTypes.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(options.Search)) {
            var searchTerm = options.Search.ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(searchTerm) || x.Description.Contains(searchTerm));
        }
        if (filter?.Required.HasValue == true) {
            query = query.Where(x => x.Required == filter.Required.Value);
        }
        var claimTypes = await query.Select(x => new ClaimTypeInfo {
            Id = x.Id,
            Name = x.Name,
            DisplayName = x.DisplayName,
            Description = x.Description,
            Rule = x.Rule,
            ValueType = x.ValueType,
            Required = x.Required,
            Reserved = x.Reserved,
            UserEditable = x.UserEditable
        })
        .ToResultSetAsync(options);
        return TypedResults.Ok(claimTypes);
    }

    internal static async Task<Results<Ok<ClaimTypeInfo>, NotFound>> GetClaimType(ExtendedConfigurationDbContext configurationDbContext, string claimTypeId) {
        var claimType = await configurationDbContext.ClaimTypes.AsNoTracking().Select(x => new ClaimTypeInfo {
            Id = x.Id,
            Name = x.Name,
            DisplayName = x.DisplayName,
            Description = x.Description,
            Rule = x.Rule,
            ValueType = x.ValueType,
            Required = x.Required,
            Reserved = x.Reserved,
            UserEditable = x.UserEditable
        })
        .FirstOrDefaultAsync(x => x.Id == claimTypeId);
        if (claimType == null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(claimType);
    }

    internal static async Task<Results<CreatedAtRoute<ClaimTypeInfo>, ValidationProblem>> CreateClaimType(ExtendedConfigurationDbContext configurationDbContext, CreateClaimTypeRequest request) {
        if (request is null) {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>() { [""] = new[] { "Request body cannot be null." } });
        }
        var exists = await configurationDbContext.ClaimTypes.AsNoTracking().AnyAsync(x => x.Name == request.Name);
        if (exists) {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>() { [nameof(CreateClaimTypeRequest.Name).Camelize()] = new[] { $"A claim type with name {request.Name} already exists." } });
        }
        var claimType = new ClaimType {
            Id = $"{Guid.NewGuid()}",
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
            Rule = request.Rule,
            ValueType = request.ValueType,
            Required = request.Required,
            Reserved = false,
            UserEditable = request.UserEditable
        };
        configurationDbContext.ClaimTypes.Add(claimType);
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.CreatedAtRoute(new ClaimTypeInfo {
            Id = claimType.Id,
            Name = claimType.Name,
            DisplayName = claimType.DisplayName,
            Description = claimType.Description,
            Rule = claimType.Rule,
            ValueType = claimType.ValueType,
            Required = claimType.Required,
            Reserved = claimType.Reserved,
            UserEditable = claimType.UserEditable
        }, nameof(GetClaimType), new { claimTypeId = claimType.Id });
    }

    internal static async Task<Results<Ok<ClaimTypeInfo>, NotFound>> UpdateClaimType(ExtendedConfigurationDbContext configurationDbContext, string claimTypeId, UpdateClaimTypeRequest request) {
        var claimType = await configurationDbContext.ClaimTypes.SingleOrDefaultAsync(x => x.Id == claimTypeId);
        if (claimType == null) {
            return TypedResults.NotFound();
        }
        // Modify claim type properties according to request model.
        claimType.DisplayName = request.DisplayName;
        claimType.Description = request.Description;
        claimType.Rule = request.Rule;
        claimType.ValueType = request.ValueType;
        claimType.Required = request.Required;
        claimType.UserEditable = !claimType.Reserved ? request.UserEditable : false;
        // Commit changes to database.
        await configurationDbContext.SaveChangesAsync();
        // Send the response.
        return TypedResults.Ok(new ClaimTypeInfo {
            Id = claimType.Id,
            Name = claimType.Name,
            DisplayName = claimType.DisplayName,
            Description = claimType.Description,
            Rule = claimType.Rule,
            ValueType = claimType.ValueType,
            Required = claimType.Required,
            Reserved = claimType.Reserved,
            UserEditable = claimType.UserEditable
        });
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> DeleteClaimType(ExtendedConfigurationDbContext configurationDbContext, string claimTypeId) {
        var claimType = await configurationDbContext.ClaimTypes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == claimTypeId);
        if (claimType == null) {
            return TypedResults.NotFound();
        }
        if (claimType.Reserved) {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>() { [nameof(CreateClaimTypeRequest.Name).Camelize()] = new[] { "Cannot delete a reserved claim type." } });
        }
        configurationDbContext.ClaimTypes.Remove(claimType);
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }
}
