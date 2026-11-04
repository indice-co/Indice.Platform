using Indice.Features.Cases.Core.Models.Requests;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Cases.Server.Endpoints;

internal static class AdminCategoriesHandlers
{
    public static async Task<Ok<ResultSet<Category>>> GetCategoriesList(ICategoryService categoryService, [AsParameters] ListOptions options) =>
        TypedResults.Ok(await categoryService.GetList(options));

    public static async Task<Results<Ok<Category>, NotFound>> GetCategoryById(ICategoryService categoryService, Guid categoryId) {
        if (await categoryService.GetById(categoryId) is not { } category) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(category);
    }

    public static async Task<CreatedAtRoute<Category>> CreateCategory(ICategoryService categoryService, CategoryRequest request) {
        var category = await categoryService.Create(request);
        return TypedResults.CreatedAtRoute(category, nameof(GetCategoryById), new { categoryId = category.Id });
    }

    public static async Task<Results<Ok<Category>, NotFound>> UpdateCategory(ICategoryService categoryService, Guid categoryId, CategoryRequest request) {
        if (await categoryService.Update(categoryId, request) is not { } category) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(category);
    }

    public static async Task<Results<NoContent, NotFound>> DeleteCategory(ICategoryService categoryService, Guid categoryId) {
        if (!await categoryService.Delete(categoryId)) {
            return TypedResults.NotFound();
        }
        return TypedResults.NoContent();
    }
}
