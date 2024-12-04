using System.Linq;
using System.Linq.Expressions;
using Indice.Events;
using Indice.Extensions;
using Indice.Features.Media.AspNetCore.Data.Models;
using Indice.Features.Media.AspNetCore.Events;
using Indice.Features.Media.AspNetCore.Stores.Abstractions;
using Indice.Features.Media.Data;
using Indice.Text;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Media.AspNetCore.Stores;

/// <summary>An implementation of <see cref="IMediaFolderStore"/> for Entity Framework Core.</summary>
internal class MediaFolderStore : IMediaFolderStore
{
    private readonly MediaDbContext _dbContext;
    private readonly IPlatformEventService _platformEventService;

    /// <summary>Creates a new instance of <see cref="MediaFolderStore"/>.</summary>
    /// <param name="dbContext">The <see cref="DbContext"/> for Media API feature.</param>
    /// <param name="platformEventService">Used to dispatch/publish messages of type <see cref="IPlatformEvent"/></param>
    /// <exception cref="ArgumentNullException"></exception>
    public MediaFolderStore(MediaDbContext dbContext, IPlatformEventService platformEventService) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _platformEventService = platformEventService ?? throw new ArgumentNullException(nameof(platformEventService));
    }

    /// <inheritdoc/>
    public async Task<DbMediaFolder?> GetById(Guid id) {
        return await _dbContext.Folders
            .Where(f => f.Id == id && !f.IsDeleted)
            .FirstOrDefaultAsync();
    }
    /// <inheritdoc/>
    public async Task<List<DbMediaFolder>> GetList(Expression<Func<DbMediaFolder, bool>>? query = null) {
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
    public async Task<Guid> Create(DbMediaFolder folder) {
        folder.Path = await FindPathAsync(_dbContext, folder.ParentId, folder.Name);
        _dbContext.Folders.Add(folder);
        await _dbContext.SaveChangesAsync();
        return folder.Id;
    }
    /// <inheritdoc/>
    public async Task Update(DbMediaFolder folder) {

        folder.Path = await FindPathAsync(_dbContext, folder.ParentId, folder.Name);
        var existingPath = await _dbContext.Folders.Where(x => x.Id == folder.Id).Select(x => x.Path).FirstOrDefaultAsync() ?? "/";
        _dbContext.Update(folder);
        await _dbContext.SaveChangesAsync();
        // change paths
        var rows = await _dbContext.Folders.Where(x => x.ParentId == folder.Id || x.Path.StartsWith(existingPath))
                                .ExecuteUpdateAsync(x => x.SetProperty(child => child.Path, child => child.Path.Replace(existingPath, folder.Path)));
        rows = await _dbContext.Files.Where(x => x.FolderId == folder.Id || x.Path.StartsWith(existingPath))
                                .ExecuteUpdateAsync(x => x.SetProperty(child => child.Path, child => child.Path.Replace(existingPath, folder.Path)));
        await _platformEventService.Publish(new FolderRenameCommand(folder.Id, existingPath, folder.Path));
    }

    public static async Task<string> FindPathAsync(MediaDbContext dbContext, Guid? parentId, string segmentName) {
        return (await FindPathsAsync(dbContext, parentId, [segmentName]))[0];
    }

    public static async Task<string[]> FindPathsAsync(MediaDbContext dbContext, Guid? parentId, IEnumerable<string> segmentNames) {
        var parentPath = "/";
        if (parentId.HasValue) {
            parentPath = await dbContext.Folders.Where(x => x.Id == parentId).Select(x => x.Path).FirstOrDefaultAsync() ?? "/";
        }
        var paths = segmentNames.Select(name => {
            var extension = Path.GetExtension(name);
            var segment = Greeklish.Translate(Path.GetFileNameWithoutExtension(name)).Unidecode().ToKebabCase();
            if (Path.HasExtension(name)) {
                return $"{parentPath.TrimEnd('/')}/{segment}{extension}";
            }
            return $"{parentPath.TrimEnd('/')}/{segment}/";
        }).ToArray();
        return paths;
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

