using System.Text.Json;
using Indice.Features.Media.AspNetCore.Data.Models;
using Indice.Features.Media.AspNetCore.Models;
using Indice.Features.Media.AspNetCore.Models.Commands;
using Indice.Features.Media.AspNetCore.Services;
using Indice.Features.Media.AspNetCore.Services.Hosting;
using Indice.Features.Media.AspNetCore.Stores.Abstractions;
using Indice.Serialization;
using Indice.Services;
using Indice.Types;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;

namespace Indice.Features.Media.AspNetCore;
/// <summary>A manager class that helps work with the Media Library API infrastructure.</summary>
/// <remarks>Creates a new instance of <see cref="MediaManager"/>.</remarks>
public class MediaManager(
    IMediaFileStore fileStore,
    IMediaFolderStore folderStore,
    IMediaSettingService settingService,
    IFileServiceFactory fileServiceFactory,
    IDistributedCache cache,
    IConfiguration configuration,
    IOptions<MediaApiOptions> mediaApiOptions
    )
{
    private const string STRUCT_CACHE_KEY = "FOLDER_STRUCTURE";
    private const string CONTENT_CACHE_KEY = "FOLDER_CONTENT";

    private readonly IMediaFileStore _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
    private readonly IMediaFolderStore _folderStore = folderStore ?? throw new ArgumentNullException(nameof(folderStore));
    private readonly IMediaSettingService _settingService = settingService ?? throw new ArgumentNullException(nameof(settingService));
    private readonly IFileService _fileService = fileServiceFactory.Create(KeyedServiceNames.FileServiceKey) ?? throw new ArgumentNullException(nameof(fileServiceFactory));
    private readonly IDistributedCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private readonly MediaApiOptions _mediaApiOptions = mediaApiOptions?.Value ?? throw new ArgumentNullException(nameof(mediaApiOptions));
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    /// <summary>Retrieves the content of a folder.</summary>
    /// <param name="folderId">The Id of the folder. Null value corresponds to Root folder.</param>
    /// <param name="page">The page number.</param>
    /// <param name="size">The page size.</param>
    public async Task<FolderContent?> GetFolderContent(Guid? folderId, int? page, int? size) {
        if (!page.HasValue || page.Value <= 0) {
            page = 1;
        }
        if (!size.HasValue || size.Value <= 0) {
            size = 10;
        }
        var structure = await GetFolderTreeStructure();
        if (structure.IsEmpty && folderId.HasValue) {
            return new FolderContent();
        }
        var folderTree = structure.FindSubTree(folderId);
        if (folderTree == null) {
            return new FolderContent();
        }
        var pagedFolders = folderTree.GetChildren(page.Value, size.Value);
        var pagedFoldersCount = pagedFolders.Count;
        var files = await ListFiles(folderId);
        var pagedFiles = new List<MediaFile>();
        if (pagedFoldersCount < size.Value) {
            var newPage = page.Value - (folderTree.TotalCount / size.Value + 1);
            var filesToSkip = newPage*(size.Value - (pagedFoldersCount > 0 ? pagedFoldersCount : folderTree.TotalCount % size.Value));
            pagedFiles = files.Skip(filesToSkip).Take(size.Value-pagedFoldersCount).ToList();
        }
        return new FolderContent {
            Id = folderId,
            Name = folderTree.Node.Name,
            ParentId = folderTree.Node.ParentId,
            Files = pagedFiles,
            Folders = pagedFolders,
            TotalCount = folderTree.TotalCount + files.Count,
        };
    }

    /// <summary>Lists the all the existing folders.</summary>
    public async Task<List<MediaFolder>> ListFolders() {
        var dbFolders = await _folderStore.GetList(f => !f.IsDeleted);
        if (dbFolders == null) {
            return [];
        }
        return dbFolders.Select(f => f.ToFolder()).ToList();
    }

    /// <summary>Details about a folder.</summary>
    /// <param name="id">The Id of the folder.</param>
    public async Task<MediaFolder?> GetFolderById(Guid id) {
        var dbFolder = await _folderStore.GetById(id);
        if (dbFolder == null) {
            return null;
        }
        return dbFolder.ToFolder();
    }

    /// <summary>Retrieves a tree structure of folders.</summary>
    public async Task<FolderTreeStructure> GetFolderTreeStructure() {
        var serializationOptions = JsonSerializerOptionDefaults.GetDefaultSettings();
        var structure = await _cache.GetAsync<FolderTreeStructure>(STRUCT_CACHE_KEY, serializationOptions);
        if (structure == null) {
            var folders = await ListFolders();
            var rootFiles = await ListFiles();
            structure = new FolderTreeStructure(folders, rootFiles.Count);
            await _cache.SetStringAsync(STRUCT_CACHE_KEY, JsonSerializer.Serialize(structure, serializationOptions), new DistributedCacheEntryOptions {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });
        }
        return structure;
    }

    /// <summary>Creates a new folder.</summary>
    /// <param name="command">The command with the required details to create a new folder.</param>
    public async Task<Guid> CreateFolder(CreateFolderCommand command) {
        var folderId = await _folderStore.Create(command.ToDbFolder());
        await _cache.RemoveAsync(STRUCT_CACHE_KEY);

        var cacheKey = $"{CONTENT_CACHE_KEY}_{(command.ParentId.HasValue ? command.ParentId.Value : "root")}";
        await _cache.RemoveAsync(cacheKey);
        return folderId;
    }

    /// <summary>Updates an existing folder.</summary>
    /// <param name="command">The command with the required details to update an existing folder.</param>
    public async Task UpdateFolder(UpdateFolderCommand command) {
        var folder = await _folderStore.GetById(command.Id) ?? throw MediaLibraryExceptions.FolderNotFound(command.Id);
        folder.Update(command);
        await _folderStore.Update(folder);
        await _cache.RemoveAsync(STRUCT_CACHE_KEY);
        var cacheKey = $"{CONTENT_CACHE_KEY}_{(command.ParentId.HasValue ? command.ParentId.Value : "root")}";
        await _cache.RemoveAsync(cacheKey);
    }

    /// <summary>Marks an existing folder and the containing elements as deleted.</summary>
    /// <param name="id">The folder id.</param>
    public async Task DeleteFolder(Guid id) {
        var folder = await _folderStore.GetById(id) ?? throw MediaLibraryExceptions.FolderNotFound(id);
        //await _folderService.Delete(id);
        var affectedFolderIds = new List<Guid>() { folder.Id };
        var structure = await GetFolderTreeStructure();
        if (structure != null) {
            var deletedFolder = structure.FindSubTree(id);
            if (deletedFolder != null) {
                affectedFolderIds.AddRange(deletedFolder.Flatten().Where(f => f.Id != id).Select(f => f.Id));
            }
        }
        var affectedFiles = await _fileStore.GetList(f => f.FolderId.HasValue && affectedFolderIds.Contains(f.FolderId.Value));
        if (affectedFiles != null) {
            await _fileStore.MarkAsDeletedRange(affectedFiles.Select(f => f.Id).ToList());
        }
        await _folderStore.MarkAsDeletedRange(affectedFolderIds);
        await _cache.RemoveAsync(STRUCT_CACHE_KEY);
        foreach (var folderId in affectedFolderIds) {
            var cacheKey = $"{CONTENT_CACHE_KEY}_{folderId}";
            await _cache.RemoveAsync(cacheKey);
        }
    }

    /// <summary>Lists the all the existing files of a folder.</summary>
    public async Task<List<MediaFile>> ListFiles(Guid? folderId = null) {
        var cacheKey = $"{CONTENT_CACHE_KEY}_{(folderId.HasValue ? folderId.Value : "root")}";
        var serializationOptions = JsonSerializerOptionDefaults.GetDefaultSettings();
        var files = await _cache.GetAsync<List<MediaFile>>(cacheKey, serializationOptions);
        if (files == null) {
            var dbfiles = await _fileStore.GetList(f => f.FolderId == folderId && !f.IsDeleted);
            files = [];
            if (dbfiles != null) {
                var cdnUrl = await _settingService.GetSetting(MediaSetting.CDN.Key);
                var permaLinkBaseUrl = string.IsNullOrWhiteSpace(cdnUrl?.Value)
                    ? Path.Combine(_configuration.GetHost()!.TrimEnd('/'),
                                   _mediaApiOptions.PathPrefix.ToString().Trim('/'),
                                   "media-root").Replace('\\', '/')
                    : cdnUrl.Value.TrimEnd('/');
                files = dbfiles.Select(f => f.ToFileDetails(permaLinkBaseUrl)).ToList();
            }
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(files, serializationOptions), new DistributedCacheEntryOptions {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });
        }
        return files;
    }

    /// <summary>Uploads a file in the system.</summary>
    /// <param name="command">The command containing all the required data to upload a file.</param>
    public async Task<Guid> UploadFile(UploadFileCommand command) {
        var dbFile = Mapper.ToDbFile(command);
        var fileId = await _fileStore.Create(dbFile);
        using (var stream = command.OpenReadStream()) {
            await _fileService.SaveAsync($"media/{dbFile.Path.TrimStart('/')}", stream);
        }
        var cacheKey = $"{CONTENT_CACHE_KEY}_{(command.FolderId.HasValue ? command.FolderId.Value : "root")}";
        await _cache.RemoveAsync(cacheKey);
        await _cache.RemoveAsync(STRUCT_CACHE_KEY);
        return fileId;
    }

    /// <summary>Uploads a file in the system.</summary>
    /// <param name="commands">The command containing all the required data to upload a file.</param>
    public async Task<List<Guid>> UploadFiles(List<UploadFileCommand> commands) {
        var dbFiles = commands.Select(Mapper.ToDbFile).ToList();
        var fileIds = await _fileStore.CreateMany(dbFiles);
        var cacheKey = $"{CONTENT_CACHE_KEY}_";
        var uploadTasks = new List<Task>();
        for (var i = 0; i <  commands.Count; i++) {
            var save = async () => {
                using (var stream = commands[i].OpenReadStream()) {
                    await _fileService.SaveAsync($"media/{dbFiles[i].Path.TrimStart('/')}", stream);
                }
            };
            uploadTasks.Add(save());
            cacheKey = $"{CONTENT_CACHE_KEY}_{(commands[i].FolderId.HasValue ? commands[i].FolderId!.Value : "root")}";
        }
        await Task.WhenAll(uploadTasks);
        await _cache.RemoveAsync(cacheKey);
        await _cache.RemoveAsync(STRUCT_CACHE_KEY);
        return fileIds;
    }

    /// <summary>Updates metadata about a file.</summary>
    /// <param name="command">The command containing all the required data to update a file.</param>
    public async Task UpdateFileMetadata(UpdateFileMetadataCommand command) {
        var dbFile = await _fileStore.GetById(command.Id) ?? throw MediaLibraryExceptions.FileNotFound(command.Id);
        if (dbFile.FolderId != command.FolderId) {
            var previousFolderCacheKy = $"{CONTENT_CACHE_KEY}_{(dbFile.FolderId.HasValue ? dbFile.FolderId.Value : "root")}";
            await _cache.RemoveAsync(previousFolderCacheKy);
        }
        dbFile.Update(command);
        await _fileStore.Update(dbFile);
        var cacheKey = $"{CONTENT_CACHE_KEY}_{(dbFile.FolderId.HasValue ? dbFile.FolderId.Value : "root")}";
        await _cache.RemoveAsync(cacheKey);
        await _cache.RemoveAsync(STRUCT_CACHE_KEY);
    }

    /// <summary>Marks an existing file as deleted.</summary>
    /// <param name="id">The file id.</param>
    public async Task DeleteFile(Guid id) {
        var dbFile = await _fileStore.GetById(id);
        if (dbFile is null) {
            throw MediaLibraryExceptions.FileNotFound(id);
        }
        dbFile.IsDeleted = true;
        await _fileStore.Update(dbFile);
        var cacheKey = $"{CONTENT_CACHE_KEY}_{(dbFile.FolderId.HasValue ? dbFile.FolderId.Value : "root")}";
        await _cache.RemoveAsync(cacheKey);
        await _cache.RemoveAsync(STRUCT_CACHE_KEY);
    }

    /// <summary>Retrieves the details about a file.</summary>
    /// <param name="path">The file location.</param>
    /// <param name="includeData">Indicates if the details should contain the byte array of data.</param>
    public async Task<MediaFile?> GetFileDetails(string path, bool? includeData) {
        var file = await _fileStore.GetByPath(path);
        return await GetFileDetailsInternal(file, includeData);
    }


    /// <summary>Retrieves the details about a file.</summary>
    /// <param name="id">The file id.</param>
    /// <param name="includeData">Indicates if the details should contain the byte array of data.</param>
    public async Task<MediaFile?> GetFileDetails(Guid id, bool? includeData) {
        var file = await _fileStore.GetById(id);
        return await GetFileDetailsInternal(file, includeData);
    }

    /// <summary>Discovers files from persistent storage.</summary>
    /// <remarks>Used to discover files that exist in the storage but not in the database, and add them to the database. 
    /// This does not involve actual data and is limited to metadata and structure.</remarks>
    public async Task DiscoverStructure() {
        // discard all files and folders marked as deleted
        await CleanUpFolders();
        // 0. Get all file paths from the storage.
        var storageFilePaths = await _fileService.SearchAsync("media");
        var dbFolders = await _folderStore.GetList(f => !f.IsDeleted);
        var storageFolders = storageFilePaths.Select(p => Path.GetDirectoryName(p)?.Replace('\\', '/').Substring(p.StartsWith("/media") ? 6 : 0) + '/').Where(p => !string.IsNullOrEmpty(p)).Distinct().ToList();
        var newFolders = storageFolders.Except(dbFolders.Select(f => f.Path)).FilterOutNulls().ToList();
        // 1. merge folder structure with existing database folder structure.
        // 2. Then clear all existing files in the database that are not in the storage,
        // 3. and add all files that are in the storage but not in the database.
        // 4. Try to invalidate cache entries for folders that are affected by the changes.
        var currentPathBuilder = new System.Text.StringBuilder();
        foreach (var folder in newFolders) {
            var pathSegments = folder.Split('/', StringSplitOptions.RemoveEmptyEntries);
            Guid? parentId = null;
            currentPathBuilder.Clear();
            currentPathBuilder.Append('/');
            foreach (var segment in pathSegments) {
                var cacheKey = $"{CONTENT_CACHE_KEY}_{(parentId.HasValue ? parentId.Value : "root")}";
                await _cache.RemoveAsync(cacheKey);
                currentPathBuilder.Append(segment).Append('/');
                var currentPath = currentPathBuilder.ToString();
                var existingFolder = dbFolders.FirstOrDefault(f => f.Path == currentPath);
                if (existingFolder == null) {
                    var newFolder = new DbMediaFolder {
                        Name = segment,
                        ParentId = parentId,
                        Path = currentPath
                    };
                    var newFolderId = await _folderStore.Create(newFolder, normalizePath: false);
                    parentId = newFolderId;
                    dbFolders.Add(newFolder);
                } else {
                    parentId = existingFolder.Id;
                }
            }
        }
        await CleanUpFiles();

        var existingFilePaths = (await _fileStore.GetList(f => !f.IsDeleted))
            .Select(f => f.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var filesToStore = new List<DbMediaFile>();
        foreach (var file in storageFilePaths) {
            var filePath = file.Replace('\\', '/').Substring(file.StartsWith("/media") ? 6 : 0);
            if (existingFilePaths.Contains(filePath)) {
                continue;
            }
            var folderPath = Path.GetDirectoryName(filePath)?.Replace('\\', '/') + '/';
            var parentId = dbFolders.FirstOrDefault(f => f.Path == folderPath)?.Id;
            var metadata = await _fileService.GetPropertiesAsync(file);
            if (metadata == null) {
                continue;
            }
            filesToStore.Add(new DbMediaFile {
                Name = Path.GetFileName(file),
                FolderId = parentId,
                Path = filePath,
                ContentLength = (int)metadata.Length,
                ContentType = metadata.ContentType,
                FileExtension = Path.GetExtension(file),
            });
        }
        if (filesToStore.Count > 0) {
            await _fileStore.CreateMany(filesToStore, normalizePath: false);
        }
        await _cache.RemoveAsync(STRUCT_CACHE_KEY);
    }

    private async Task<MediaFile?> GetFileDetailsInternal(DbMediaFile? file, bool? includeData) {
        if (file == null) {
            return null;
        }
        if (includeData.HasValue && includeData.Value) {
            var path = $"media/{file.Path.TrimStart('/')}";
            var data = await _fileService.GetAsync(path);
            if (data is null) {
                return null;
            }
            file.Data = data;
        }
        var cdnUrl = await _settingService.GetSetting(MediaSetting.CDN.Key);
        var permaLinkBaseUrl = string.IsNullOrWhiteSpace(cdnUrl?.Value)
                    ? $"{_configuration.GetHost()!.TrimEnd('/')}/{_mediaApiOptions.PathPrefix.ToString().Trim('/')}/media-root"
                    : $"{cdnUrl.Value.TrimEnd('/')}";
        return file.ToFileDetails(permaLinkBaseUrl);
    }

    /// <summary>Deletes all files marked as deleted. Used by <see cref="FilesCleanUpHostedService"/></summary>
    internal async Task CleanUpFiles(bool onlyMarkedAsDeleted = true) {
        var deletedFiles = onlyMarkedAsDeleted ? await _fileStore.GetList(f => f.IsDeleted) : await _fileStore.GetList();
        if (deletedFiles is null) {
            return;
        }
        foreach (var deletedFile in deletedFiles) {
            try {
                await _fileService.DeleteAsync(deletedFile.Path);
            } catch (FileNotFoundServiceException) {
                // ignore
            } finally {
                await _fileStore.Delete(deletedFile.Id);
            }
        }
    }

    /// <summary>Deletes all folders marked as deleted. Used by <see cref="FoldersCleanUpHostedService"/></summary>
    internal async Task CleanUpFolders(bool onlyMarkedAsDeleted = true) {
        var deletedFolders = onlyMarkedAsDeleted ? await _folderStore.GetList(f => f.IsDeleted) : await _folderStore.GetList();
        if (deletedFolders is null) {
            return;
        }
        var structure = new FolderTreeStructure(deletedFolders.Select(f => f.ToFolder()));
        foreach (var item in structure.Items) {
            while (!item.IsLeaf) {
                foreach (var folder in item.RemoveLeaves()) {
                    await _folderStore.Delete(folder.Node.Id);
                }
            }
            await _folderStore.Delete(item.Node.Id);
        }
    }
}
