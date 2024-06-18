using System.Linq.Expressions;
using Indice.Features.Media.AspNetCore.Data.Models;
using Indice.Features.Media.AspNetCore.Stores;
using Indice.Features.Media.AspNetCore.Stores.Abstractions;
using Indice.Features.Media.Data;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Media.AspNetCore.Services;

/// <summary>An implementation of <see cref="IMediaFileStore"/> for Entity Framework Core.</summary>
internal class MediaFileStore : IMediaFileStore
{
    private readonly MediaDbContext _dbContext;

    /// <summary>Creates a new instance of <see cref="MediaFileStore"/>.</summary>
    /// <param name="dbContext">The <see cref="DbContext"/> for Media API feature.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public MediaFileStore(MediaDbContext dbContext) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }
    /// <inheritdoc/>
    public async Task<DbMediaFile?> GetById(Guid id) {
        return await _dbContext
            .Files
            .Where(x => x.Id == id && (x.Folder == null || !x.Folder.IsDeleted))
            .SingleOrDefaultAsync();
    }
    /// <inheritdoc/>
    public async Task<List<DbMediaFile>> GetList(Expression<Func<DbMediaFile, bool>>? query = null) {
        query ??= f => true;
        return await _dbContext.Files
            .Where(query)
            .OrderBy(f => f.FolderId)
            .ThenBy(f => f.Name)
            .ToListAsync();
    }
    /// <inheritdoc/>
    public async Task<List<DbMediaFile>> ListFiles(List<Guid> folderIds) {
        return await _dbContext.Files.Where(f => f.FolderId.HasValue && folderIds.Contains(f.FolderId.Value)).ToListAsync();
    }
    /// <inheritdoc/>
    public async Task<Guid> Create(DbMediaFile file) {
        file.Path = await MediaFolderStore.FindPathAsync(_dbContext, file.FolderId, file.Name);
        _dbContext.Files.Add(file);
        await _dbContext.SaveChangesAsync();
        return file.Id;
    }
    /// <inheritdoc/>
    public async Task Update(DbMediaFile file) {
        file.Path = await MediaFolderStore.FindPathAsync(_dbContext, file.FolderId, file.Name);
        _dbContext.Files.Update(file);
        await _dbContext.SaveChangesAsync();
    }
    /// <inheritdoc/>
    public async Task Delete(Guid fileId) {
        await _dbContext.Files.Where(f => f.Id == fileId).ExecuteDeleteAsync();
    }
    /// <inheritdoc/>
    public async Task MarkAsDeletedRange(List<Guid> ids) {
        await _dbContext.Files.Where(_ => ids.Contains(_.Id)).ExecuteUpdateAsync(f => f.SetProperty(p => p.IsDeleted, true));
    }
}
