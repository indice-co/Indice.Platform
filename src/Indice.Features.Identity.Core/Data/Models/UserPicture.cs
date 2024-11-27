namespace Indice.Features.Identity.Core.Data.Models;

/// <summary>A user profile picture will be stored here.</summary>
public class UserPicture
{
    /// <summary>Constructs a new instance of <see cref="UserPicture"/> with a new <see cref="Guid"/> as Id.</summary>
    public UserPicture() : this(Guid.NewGuid()) { }

    /// <summary>Constructs a new instance of <see cref="UserPicture"/> using the given <see cref="Guid"/> as Id.</summary>
    /// <param name="id">The primary key.</param>
    public UserPicture(Guid id) => Id = id;

    /// <summary>The primary key.</summary>
    public Guid Id { get; }
    /// <summary>The user id related.</summary>
    public string UserId { get; set; } = null!;
    /// <summary>Picture hash.</summary>
    public string PictureKey { get; set; } = null!;
    /// <summary>Content Type.</summary>
    public string ContentType { get; set; } = null!;
    /// <summary>Content Length.</summary>
    public int ContentLength { get; set; }
    /// <summary>The data bytes.</summary>
    public byte[] Data { get; set; } = [];
    /// <summary>Created date</summary>
    public DateTimeOffset CreatedDate { get; set; }
    /// <summary>Password hash.</summary>
    public string? LoginProvider { get; set; }
}
