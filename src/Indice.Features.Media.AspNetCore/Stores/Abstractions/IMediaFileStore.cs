using System.Linq.Expressions;
using Indice.Features.Media.AspNetCore.Data.Models;

namespace Indice.Features.Media.AspNetCore.Stores.Abstractions;

/// <summary>The Media File's store.</summary>
public interface IMediaFileStore
{
    /// <summary>Retrieves a file by Id.</summary>
    /// <param name="id">The file Id.</param>
    Task<DbMediaFile?> GetById(Guid id);
    /// <summary>Retrieves a file by its location in the tree .</summary>
    /// <param name="path">The files location.</param>
    Task<DbMediaFile?> GetByPath(string path);
    /// <summary>Retrieves a a list of all files.</summary>
    /// <param name="query">The query to limit results.</param>
    Task<List<DbMediaFile>> GetList(Expression<Func<DbMediaFile, bool>>? query = null);
    /// <summary>Creates a new file.</summary>
    /// <param name="file">The file.</param>
    Task<Guid> Create(DbMediaFile file);
    /// <summary>Updates an existing file.</summary>
    /// <param name="file">The file.</param>
    Task Update(DbMediaFile file);
    /// <summary>Deletes an existing file.</summary>
    /// <param name="fileId">The file Id.</param>
    Task Delete(Guid fileId);
    /// <summary>Marks the selected files as deleted.</summary>
    /// <param name="ids">The file Ids.</param>
    Task MarkAsDeletedRange(List<Guid> ids);
}
