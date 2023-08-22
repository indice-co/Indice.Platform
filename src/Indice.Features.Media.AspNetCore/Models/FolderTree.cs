namespace Indice.Features.Media.AspNetCore.Models;

/// <summary>Represent a Tree of Folders.</summary>
public class FolderTree
{
    /// <summary>The root element of the tree.</summary>
    public Folder Node { get; set; }
    /// <summary>The element's children.</summary>
    public List<FolderTree> Children { get; set; }
    /// <summary>Indicates that this is a root node.</summary>
    public bool IsRoot => Node.ParentId == null;
    /// <summary>Indicates that this is a leaf node.</summary>
    public bool IsLeaf => Children == null || !Children.Any();
    /// <summary>The number of children.</summary>
    public int TotalCount => IsLeaf ? 0 : Children.Count;

    /// <summary>Constructs an empty <see cref="FolderTree"/>.</summary>
    public FolderTree() {
        Node = new Folder {
            Name = "",
            CreatedBy = "",
        };
        Children = new List<FolderTree>();
    }
    /// <summary>Constructs an <see cref="FolderTree"/>.</summary>
    /// <param name="root">The root folder.</param>
    /// <param name="folders">The folder to create the tree.</param>
    public FolderTree(Folder root, IEnumerable<Folder> folders) {
        Node = root ?? throw new ArgumentNullException(nameof(root));
        Children = new List<FolderTree>();
        Build(folders);
    }
    /// <summary>Builds the Tree.</summary>
    /// <param name="folders">The items of the Tree.</param>
    public void Build(IEnumerable<Folder> folders) {
        if (folders == null || !folders.Any()) {
            return;
        }
        foreach (var folder in folders.Where(f => f.ParentId == Node.Id)) {
            Children.Add(new FolderTree(folder, folders));
        }
    }
    /// <summary>Flattens the Tree.</summary>
    public IEnumerable<Folder> Flatten() {
        return new List<Folder>() {Node}.Concat(Children.SelectMany(c => c.Flatten()));
    }
    /// <summary>Searches an element of the specified Id.</summary>
    /// <param name="id">The item Id.</param>
    public FolderTree? FindChildById(Guid id) {
        var child = Children.FirstOrDefault(c => c.Node.Id == id);
        if (child == null && Children.Any()) {
            return Children.Select(c => c.FindChildById(id)).FirstOrDefault(c => c?.Node?.Id is not null);
        }
        return child;
    }
    /// <summary> Traverses the tree, returns and removes the leaves.</summary>
    public List<FolderTree> RemoveLeaves() {
        var leafChildren = Children.Where(c => c.IsLeaf).ToList();
        Children = Children.Where(c => !c.IsLeaf).ToList();
        return leafChildren.Concat(Children.SelectMany(c => c.RemoveLeaves())).ToList();
    }



    /// <summary>Retrieves paged result of children.</summary>
    /// <param name="page">The item Id.</param>
    /// <param name="size">The item Id.</param>
    /// <returns>A list of <see cref="Folder"/></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public List<Folder> GetChildren(int page, int size) {
        if (page <= 0) {
            throw new ArgumentOutOfRangeException(nameof(page));
        }
        if (size <= 0) {
            throw new ArgumentOutOfRangeException(nameof(page));
        }
        if (IsLeaf) {
            return new List<Folder>();
        }
        return Children.Select(c => c.Node).Skip((page - 1) * size).Take(size).ToList();
    }
}

