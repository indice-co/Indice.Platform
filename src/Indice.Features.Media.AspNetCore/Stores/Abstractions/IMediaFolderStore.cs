using System.Linq.Expressions;
using Indice.Features.Media.AspNetCore.Data.Models;

namespace Indice.Features.Media.AspNetCore.Stores.Abstractions;

/// <summary>The Media Folder's store.</summary>
public interface IMediaFolderStore
{
    /// <summary>Gets a folder by it's unique id.</summary>
    /// <param name="id">The id of the folder.</param>
    Task<DbMediaFolder?> GetById(Guid id);
    /// <summary>Retreieves all folders.</summary>
    /// <param name="query">The query to limit the results.</param>
    Task<List<DbMediaFolder>> GetList(Expression<Func<DbMediaFolder, bool>>? query = null);
    /// <summary>Creates a new folder.</summary>
    /// <param name="folder">The data for the folder to create.</param>
    Task<Guid> Create(DbMediaFolder folder);
    /// <summary>Updates an existing folder.</summary>
    /// <param name="folder">The data for the folder to update.</param>
    /// <param name="updateReferences">Forces recalculation of file paths</param>
    Task Update(DbMediaFolder folder, bool updateReferences = false);
    /// <summary>Deletes an existing folder.</summary>
    /// <param name="id">The id of the folder.</param>
    Task Delete(Guid id);
    /// <summary>Marks the folders as deleted.</summary>
    /// <param name="ids">The ids of the folders to be marked as deleted.</param>
    Task MarkAsDeletedRange(List<Guid> ids);
}