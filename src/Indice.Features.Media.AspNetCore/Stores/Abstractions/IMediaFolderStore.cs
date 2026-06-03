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
    /// <param name="normalizePath">Will go about normalizing the structure path according to parent structure, naming conventions and url safe rules. Defaults to true.
    Task<Guid> Create(DbMediaFolder folder, bool normalizePath = true);
    /// <summary>Updates an existing folder.</summary>
    /// <param name="folder">The data for the folder to update.</param>
    Task Update(DbMediaFolder folder);
    /// <summary>Deletes an existing folder.</summary>
    /// <param name="id">The id of the folder.</param>
    Task Delete(Guid id);
    /// <summary>Marks the folders as deleted.</summary>
    /// <param name="ids">The ids of the folders to be marked as deleted.</param>
    Task MarkAsDeletedRange(List<Guid> ids);
}