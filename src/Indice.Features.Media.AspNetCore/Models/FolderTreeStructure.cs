using Indice.Features.Media.AspNetCore.Models;

/// <summary>Represent a Tree Hierarchical Structure of Folders.</summary>
public class FolderTreeStructure
{
    /// <summary>Constructs an empty <see cref="FolderTreeStructure"/>.</summary>
    public FolderTreeStructure() {
        Items = new List<FolderTree>();
    }
    /// <summary>Constructs a <see cref="FolderTreeStructure"/>.</summary>
    /// <param name="folders">The items of the structure.</param>
    /// <param name="rootFilesCount">The root files count.</param>
    public FolderTreeStructure(IEnumerable<MediaFolder> folders, int rootFilesCount = 0) {
        Items = new List<FolderTree>();
        RootFilesCount = rootFilesCount;
        Build(folders);
    }
    /// <summary>The structure first level items.</summary>
    public List<FolderTree> Items { get; set; }
    /// <summary>The structure first files count.</summary>
    public int RootFilesCount { get; set; }
    /// <summary>Indicates if the structure is Empty.</summary>
    public bool IsEmpty => Items == null || !Items.Any();
    /// <summary>The number of elements.</summary>
    public int TotalCount => IsEmpty ? RootFilesCount : Items.Count + RootFilesCount;
    /// <summary>The number of files the structure contains.</summary>
    public int FilesCount => IsEmpty ? RootFilesCount : (Items.Select(i => i.TotalFilesCount).Sum() ?? 0) + RootFilesCount;
    /// <summary>Builds the structure.</summary>
    /// <param name="folders">The items of the structure.</param>
    public void Build(IEnumerable<MediaFolder> folders) {
        if (folders == null || !folders.Any()) {
            return;
        }
        foreach (var folder in folders.Where(f => f.ParentId is null)) {
            Items.Add(new FolderTree(folder, folders));
        }
    }
    /// <summary>Flattens the structure.</summary>
    public IEnumerable<MediaFolder> Flatten() {
        return Items.SelectMany(i => i.Flatten());
    }
    /// <summary>Searches an element of the specified Id.</summary>
    /// <param name="id">The item Id.</param>
    public FolderTree? FindSubTree(Guid? id) {
        if (!id.HasValue || IsEmpty) {
            return new FolderTree() {
                Children = Items
            };
        }
        var child = Items.FirstOrDefault(c => c.Node.Id == id);
        if (child == null) {
            var flattenedItems = Flatten();
            var item = flattenedItems.FirstOrDefault(i => i.Id == id);//Items.Select(c => c.FindChildById(id.Value))?.FirstOrDefault(c => c?.Node?.Id is not null);
            if (item is not null) {
                return new FolderTree(item, flattenedItems);
            }
        }
        return child;
    }
    /// <summary>Searches an element of the specified Id and retrieves the children.</summary>
    /// <param name="id">The item Id.</param>
    public List<FolderTree>? FindChildren(Guid? id) {
        if (!id.HasValue) {
            return Items;
        }
        return FindSubTree(id.Value)?.Children;
    }
}