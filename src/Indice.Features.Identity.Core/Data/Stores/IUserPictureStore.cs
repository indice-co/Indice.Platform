using IdentityModel;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Models;
using Microsoft.AspNetCore.Identity;

namespace Indice.Features.Identity.Core.Data.Stores;

/// <summary>Provides an abstraction for a store which stores user profile pictures.</summary>
/// <typeparam name="TUser">The user type.</typeparam>
public interface IUserPictureStore<TUser> where TUser : User
{
    /// <summary>Profile Picture backing store claim type.</summary>
    internal const string PictureDataClaimType = JwtClaimTypes.Picture + "_data";
    /// <summary>Sets a new user profile picture</summary>
    /// <param name="user">The user instance.</param>
    /// <param name="inputStream">The picture stream</param>
    /// <param name="sideSize">Image side size to store</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/> of the creation operation.</returns>
    Task<IdentityResult> SetUserPictureAsync(TUser user, Stream inputStream, int sideSize = 256, CancellationToken cancellationToken = default);


    /// <summary>Clear user profile picture</summary>
    /// <param name="user">The user instance</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/> of the creation operation.</returns>
    Task<IdentityResult> ClearUserPictureAsync(TUser user, CancellationToken cancellationToken = default);


    /// <summary>Get the default user Picture stream and content type</summary>
    /// <param name="user">The user instance</param>
    /// <param name="contentType">Content type of file to be returned</param>
    /// <param name="size">Image size to be returned</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing a tupple of the output picture <see cref="Stream"/> and its Content Type.</returns>
    Task<(Stream? Stream, string ContentType)> GetUserPictureAsync(TUser user, string? contentType = null, int? size = null, CancellationToken cancellationToken = default);

    /// <summary>Check to see if a picture is assigned to the given user</summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing true if there is a picture in store, otherwize false.</returns>
    Task<bool> UserPictureExistsAsync(TUser user, CancellationToken cancellationToken = default);

    /// <summary>Get the default user Picture stream and content type</summary>
    /// <param name="pictureKey">The key assiciated with this picture. Can be the Sha256 of the userid or email</param>
    /// <param name="contentType">Content type of file to be returned</param>
    /// <param name="size">Image size to be returned</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing a tupple of the output picture <see cref="Stream"/> and its Content Type.</returns>
    Task<(Stream? Stream, string ContentType)> FindUserPictureByKeyAsync(string pictureKey, string? contentType = null, int? size = null, CancellationToken cancellationToken = default);
}
