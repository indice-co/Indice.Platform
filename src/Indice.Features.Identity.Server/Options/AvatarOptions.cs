namespace Indice.Features.Identity.Server.Options;

/// <summary>Options for the avatar management.</summary>
public class AvatarOptions
{
    private readonly HashSet<int> _allowedSizes = [ 24, 32, 48, 64, 128, 192, 256, 512 ];
    /// <summary>Controls whether the avatar management feature is enabled. Defaults to false.</summary>
    public bool Enabled { get; set; } = true;
    /// <summary>The maximum acceptable size of the files to be uploaded in bytes. Defaults to <i>1MB</i>.</summary>
    public long MaxFileSize { get; set; } = 1024 * 1024 * 3 ;
    /// <summary>The acceptable file extensions. Defaults to <i>.png, .jpg</i>.</summary>
    public string AcceptableFileExtensions => ".png, .jpg, .webp";
    /// <summary>Allowed tile sizes. Only these sizes are available.</summary>
    public ICollection<int> AllowedSizes => _allowedSizes;
}