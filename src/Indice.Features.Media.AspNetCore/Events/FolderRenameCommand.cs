using Indice.Events;

namespace Indice.Features.Media.AspNetCore.Events;

/// <summary>Folder rename event is triggered in order to traverse the tree and rename all child assets</summary>
public record FolderRenameCommand(Guid FolderId, string OldName, string NewName) : IPlatformEvent;
