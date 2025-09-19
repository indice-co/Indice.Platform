using System.Reflection.Metadata;
using Azure.Storage.Blobs.Models;
using FubarDev.FtpServer.FileSystem;
using FubarDev.FtpServer.FileSystem.Generic;

namespace Indice.Features.FtpServer.Azure;

/// <summary>Represents a file system entry (file or directory) in an Azure Blob Storage file system.</summary>
public class AzureBlobFileSystemEntry : IUnixFileSystemEntry
{
    /// <summary>Initializes a new instance of the <see cref="AzureBlobFileSystemEntry"/> class.</summary>
    public AzureBlobFileSystemEntry(AzureBlobFileSystem fileSystem, BlobHierarchyItem item) {
        FileSystem = fileSystem;
        Item = item;
        IsFolder = !Item.IsBlob;
        Permissions = new GenericUnixPermissions(
            new GenericAccessMode(true, true, IsFolder),
            new GenericAccessMode(true, true, IsFolder),
            new GenericAccessMode(true, true, IsFolder));
    }

    /// <summary>Gets a value indicating whether this entry represents a folder (directory) in the file system.</summary>
    public bool IsFolder { get; }

    /// <summary>Gets the underlying blob hierarchy item representing the file or directory.</summary>
    public BlobHierarchyItem Item { get; }

    /// <summary>Gets the file system to which this entry belongs.</summary>
    public IUnixFileSystem FileSystem { get; }

    /// <summary>Get the name of the group that owns the file or directory. Always returns "group".</summary>
    public string Group => "group";

    /// <summary>Gets the number of links to the file or directory. Always returns 1.</summary>
    public long NumberOfLinks => 1;

    /// <summary>Gets the owner of the file or directory. Always returns "owner".</summary>
    public string Owner => "owner";
    
    /// <summary>Gets the permissions of the file or directory.</summary>
    public IUnixPermissions Permissions { get; }

    /// <summary>Gets the creation time of the file or directory. Always returns null for folders.</summary>
    public DateTimeOffset? CreatedTime => IsFolder ? null : Item.Blob.Properties.CreatedOn;

    /// <summary>Gets the last access time of the file or directory. Always returns null for folders.</summary>
    public DateTimeOffset? LastWriteTime => IsFolder ? null : Item.Blob.Properties.LastModified ?? Item.Blob.Properties.CreatedOn;

    /// <summary>Gets the name of the file or directory represented by this entry.</summary>
    public string Name => IsFolder ? Path.GetFileName(Path.GetDirectoryName(Item.Prefix))!
                                   : Path.GetFileName(Item.Blob.Name);



}
