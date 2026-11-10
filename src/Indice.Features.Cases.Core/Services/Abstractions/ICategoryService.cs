using Indice.Features.Cases.Core.Models.Requests;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Types;

namespace Indice.Features.Cases.Core.Services.Abstractions;

/// <summary>The category service for managing case type <see cref="Category"/>.</summary>
public interface ICategoryService
{
    /// <summary>Get the categories list.</summary>
    /// <param name="options">List options.</param>
    Task<ResultSet<Category>> GetList(ListOptions options);

    /// <summary>Get a category by id.</summary>
    /// <param name="id">The category id.</param>
    Task<Category?> GetById(Guid id);

    /// <summary>Create a new category.</summary>
    /// <param name="request">The request.</param>
    Task<Category> Create(CategoryRequest request);

    /// <summary>Update an existing category.</summary>
    /// <param name="id">The category id.</param>
    /// <param name="request">The request.</param>
    Task<Category?> Update(Guid id, CategoryRequest request);

    /// <summary>Delete a category.</summary>
    /// <param name="id">The category id.</param>
    /// <returns>False if not found.</returns>
    Task<bool> Delete(Guid id);
}
