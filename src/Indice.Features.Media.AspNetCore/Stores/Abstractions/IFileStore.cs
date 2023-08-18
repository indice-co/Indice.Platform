using System.Linq.Expressions;
using Indice.Features.Media.AspNetCore.Data.Models;

namespace Indice.Features.Media.AspNetCore.Stores.Abstractions;

/// <summary>A service that contains media management related operations.</summary>
public interface IFileStore
{
    /// <summary>Retrieves a file by Id.</summary>
    /// <param name="id">The file Id.</param>
    Task<DbFile?> GetById(Guid id);
    /// <summary>Retrieves a a list of all files.</summary>
    /// <param name="query">The query to limit results.</param>
    Task<List<DbFile>> GetList(Expression<Func<DbFile, bool>>? query = null);
    /// <summary>Creates a new file.</summary>
    /// <param name="file">The file.</param>
    Task<Guid> Create(DbFile file);
    /// <summary>Updates an existing file.</summary>
    /// <param name="file">The file.</param>
    Task Update(DbFile file);
    /// <summary>Deletes an existing file.</summary>
    /// <param name="fileId">The file Id.</param>
    Task Delete(Guid fileId);
    /// <summary>Marks the selected files as deleted.</summary>
    /// <param name="ids">The file Ids.</param>
    Task MarkAsDeletedRange(List<Guid> ids);
}
