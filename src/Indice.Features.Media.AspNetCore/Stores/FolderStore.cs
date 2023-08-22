using System.Linq.Expressions;
using Indice.Features.Media.AspNetCore.Data.Models;
using Indice.Features.Media.AspNetCore.Stores.Abstractions;
using Indice.Features.Media.Data;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Media.AspNetCore.Stores;

/// <summary>An implementation of <see cref="IFolderStore"/> for Entity Framework Core.</summary>
internal class FolderStore : IFolderStore
{
    private readonly MediaDbContext _dbContext;

    /// <summary>Creates a new instance of <see cref="FolderStore"/>.</summary>
    /// <param name="dbContext">The <see cref="DbContext"/> for Media API feature.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public FolderStore(MediaDbContext dbContext) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc/>
    public async Task<DbFolder?> GetById(Guid id) {
        return await _dbContext.Folders
            .Where(f => f.Id == id && !f.IsDeleted)
            .FirstOrDefaultAsync();
    }
    /// <inheritdoc/>
    public async Task<List<DbFolder>> GetList(Expression<Func<DbFolder, bool>>? query = null) {
        query ??= f => !f.IsDeleted;
        return await _dbContext.Folders
            .Where(query)
            .Include(f => f.SubFolders.Where(f => !f.IsDeleted))
            .Include(f => f.Files.Where(f => !f.IsDeleted))
            .OrderBy(f => f.ParentId)
            .ThenBy(f => f.Name)
            .ToListAsync();
    }
    /// <inheritdoc/>
    public async Task<Guid> Create(DbFolder folder) {
        _dbContext.Folders.Add(folder);
        await _dbContext.SaveChangesAsync();
        return folder.Id;
    }
    /// <inheritdoc/>
    public async Task Update(DbFolder folder) {
        _dbContext.Folders.Update(folder);
        await _dbContext.SaveChangesAsync();
    }
    /// <inheritdoc/>
    public async Task Delete(Guid id) {
        await _dbContext.Folders
            .Where(f => f.Id == id)
            .ExecuteDeleteAsync();
    }
    /// <inheritdoc/>
    public async Task MarkAsDeletedRange(List<Guid> ids) {
        await _dbContext.Folders
            .Where(_ => ids.Contains(_.Id))
            .ExecuteUpdateAsync(f => f.SetProperty(p => p.IsDeleted, true));
    }
}
