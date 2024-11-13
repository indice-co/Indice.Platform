using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Indice.Extensions;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Extensions;
using Indice.Features.Identity.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Indice.Features.Identity.Core.Data.Stores;

/// <inheritdoc/>
public class ExtendedUserStore : ExtendedUserStore<IdentityDbContext, User, Role>
{
    /// <summary>Creates a new instance of <see cref="ExtendedUserStore"/>.</summary>
    /// <param name="context">The DbContext to use for the Identity framework.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="describer">Service to enable localization for application facing identity errors.</param>
    public ExtendedUserStore(IdentityDbContext context, IConfiguration configuration, IdentityErrorDescriber? describer = null) : base(context, configuration, describer) { }
}

/// <inheritdoc/>
public class ExtendedUserStore<TContext> : ExtendedUserStore<TContext, User, IdentityRole> where TContext : IdentityDbContext<User, IdentityRole>
{
    /// <summary>Creates a new instance of <see cref="ExtendedUserStore"/>.</summary>
    /// <param name="context">The DbContext to use for the Identity framework.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="describer">Service to enable localization for application facing identity errors.</param>
    public ExtendedUserStore(TContext context, IConfiguration configuration, IdentityErrorDescriber? describer = null) : base(context, configuration, describer) { }
}

/// <inheritdoc/>
public partial class ExtendedUserStore<TContext, TUser, TRole> : UserStore<TUser, TRole, TContext>, IExtendedUserStore<TUser>, IUserDeviceStore<TUser>, IUserPictureStore<TUser>
    where TContext : IdentityDbContext<TUser, TRole>
    where TUser : User
    where TRole : IdentityRole
{
    /// <summary>Creates a new instance of <see cref="ExtendedUserStore{TContext, TUser, TRole}"/>.</summary>
    /// <param name="context">The DbContext to use for the Identity framework.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="describer">Service to enable localization for application facing identity errors.</param>
    public ExtendedUserStore(TContext context, IConfiguration configuration, IdentityErrorDescriber? describer = null) : base(context, describer) {
        PasswordHistoryLimit = configuration.GetSection($"{nameof(IdentityOptions)}:{nameof(IdentityOptions.Password)}").GetValue<int?>(nameof(PasswordHistoryLimit)) ??
                               configuration.GetSection(nameof(PasswordOptions)).GetValue<int?>(nameof(PasswordHistoryLimit));
        StorePictureAsClaim = configuration.GetSection($"{nameof(IdentityOptions)}:{nameof(IdentityOptions.User)}").GetValue<bool?>(nameof(StorePictureAsClaim)) ??
                               configuration.GetSection(nameof(User)).GetValue<bool?>(nameof(StorePictureAsClaim)) ?? false;
        PasswordExpirationPolicy = configuration.GetSection($"{nameof(IdentityOptions)}:{nameof(IdentityOptions.Password)}").GetValue<PasswordExpirationPolicy?>(nameof(PasswordExpirationPolicy)) ??
                                   configuration.GetSection(nameof(PasswordOptions)).GetValue<PasswordExpirationPolicy?>(nameof(PasswordExpirationPolicy));
    }

    private DbSet<UserDevice> UserDeviceSet => Context.Set<UserDevice>();
    private DbSet<IdentityUserClaim<string>> UserClaimsSet => Context.Set<IdentityUserClaim<string>>();
    private DbSet<UserPicture> UserPictureSet => Context.Set<UserPicture>();

    /// <inheritdoc/>
    public IQueryable<UserDevice> UserDevices => UserDeviceSet.AsQueryable();
    /// <inheritdoc/>
    public int? PasswordHistoryLimit { get; protected set; }
    /// <inheritdoc/>
    public PasswordExpirationPolicy? PasswordExpirationPolicy { get; protected set; }
    /// <inheritdoc/>
    public bool StorePictureAsClaim { get; protected set; }

    #region Method Overrides
    /// <inheritdoc/>
    public override async Task SetPasswordHashAsync(TUser user, string? passwordHash, CancellationToken cancellationToken = default) {
        var changeDate = DateTime.UtcNow;
        if (PasswordHistoryLimit.HasValue && !string.IsNullOrWhiteSpace(passwordHash)) {
            var numberOfPasswordsToKeep = Math.Max(PasswordHistoryLimit.Value, 0);
            var toPurge = await Context.Set<UserPassword>()
                                       .Where(x => x.UserId == user.Id)
                                       .OrderByDescending(x => x.DateCreated)
                                       .Skip(numberOfPasswordsToKeep)
                                       .ToArrayAsync(cancellationToken);
            Context.Set<UserPassword>().RemoveRange(toPurge);
            await Context.Set<UserPassword>()
                         .AddAsync(new UserPassword {
                             UserId = user.Id,
                             DateCreated = changeDate,
                             PasswordHash = passwordHash
                         }, cancellationToken);
        }
        user.LastPasswordChangeDate = changeDate;
        // Calculate expiration date based on policy.
        user.PasswordExpirationDate = user.CalculatePasswordExpirationDate();
        await base.SetPasswordHashAsync(user, passwordHash, cancellationToken);
    }

    /// <inheritdoc/>
    public override async Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        // Calculate expiration date based on policy.
        user.PasswordExpirationDate = user.CalculatePasswordExpirationDate();
        return await base.UpdateAsync(user, cancellationToken);
    }

    /// <inheritdoc/>
    public override async Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken = default) {
        user.CreateDate = DateTimeOffset.UtcNow;
        var hasPassword = !string.IsNullOrWhiteSpace(user.PasswordHash);
        // If user does not already have a policy assigned use the default policy.
        // If the user does not have a password he is probably coming from an external provider, so no need to assign a password expiration policy.
        if (hasPassword) {
            if (!user.PasswordExpirationPolicy.HasValue) {
                user.PasswordExpirationPolicy = PasswordExpirationPolicy;
            }
            user.PasswordExpirationDate = user.CalculatePasswordExpirationDate();
        }
        return await base.CreateAsync(user, cancellationToken);
    }
    #endregion

    /// <inheritdoc/>
    public Task SetPasswordExpirationPolicyAsync(TUser user, PasswordExpirationPolicy? policy, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        // Set the policy.
        user.PasswordExpirationPolicy = policy;
        // Calculate expiration date based on policy.
        user.PasswordExpirationDate = user.CalculatePasswordExpirationDate();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetPasswordExpiredAsync(TUser user, bool expired, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        user.PasswordExpired = expired;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetLastSignInDateAsync(TUser user, DateTimeOffset? timestamp, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        user.LastSignInDate = timestamp ?? DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> CreateDeviceAsync(TUser user, UserDevice device, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        UserDeviceSet.Add(device);
        try {
            await SaveChanges(cancellationToken);
        } catch (DbUpdateConcurrencyException) {
            return IdentityResult.Failed(ErrorDescriber.ConcurrencyFailure());
        }
        return IdentityResult.Success;
    }

    /// <inheritdoc/>
    public async Task<IList<UserDevice>> GetDevicesAsync(TUser user, UserDeviceListFilter? filter = null, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        return await UserDeviceSet
            .Include(device => device.User)
            .Where(device => device.UserId == user.Id)
            .ApplyFilter(filter)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> GetDevicesCountAsync(TUser user, UserDeviceListFilter? filter = null, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        return await UserDevices
            .Where(device => device.UserId == user.Id)
            .ApplyFilter(filter)
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<UserDevice?> GetDeviceByIdAsync(TUser user, string deviceId, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        return await UserDeviceSet.Include(x => x.User).SingleOrDefaultAsync(x => x.UserId == user.Id && x.DeviceId == deviceId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> UpdateDeviceAsync(TUser user, UserDevice device, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        Context.Update(device);
        try {
            await SaveChanges(cancellationToken);
        } catch (DbUpdateConcurrencyException) {
            return IdentityResult.Failed(ErrorDescriber.ConcurrencyFailure());
        }
        return IdentityResult.Success;
    }

    /// <inheritdoc/>
    public async Task RemoveDeviceAsync(TUser user, UserDevice device, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        UserDeviceSet.Remove(device);
        await SaveChanges(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> SetNativeDevicesRequirePasswordAsync(TUser user, bool requiresPassword, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        var devices = await GetDevicesAsync(user, UserDeviceListFilter.NativeDevices(), cancellationToken: cancellationToken);
        foreach (var device in devices) {
            device.RequiresPassword = requiresPassword;
        }
        try {
            await SaveChanges(cancellationToken);
        } catch (DbUpdateConcurrencyException) {
            return IdentityResult.Failed(ErrorDescriber.ConcurrencyFailure());
        }
        return IdentityResult.Success;
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> SetBrowsersMfaSessionExpirationDate(TUser user, DateTimeOffset? expirationDate, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        var devices = await GetDevicesAsync(user, UserDeviceListFilter.TrustedBrowsers(), cancellationToken: cancellationToken);
        foreach (var device in devices) {
            device.MfaSessionExpirationDate = expirationDate;
        }
        try {
            await SaveChanges(cancellationToken);
        } catch (DbUpdateConcurrencyException) {
            return IdentityResult.Failed(ErrorDescriber.ConcurrencyFailure());
        }
        return IdentityResult.Success;
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> RemoveAllClaimsAsync(TUser user, string claimType, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        var claimsToRemove = await UserClaimsSet.Where(x => x.ClaimType == claimType && x.UserId == user.Id).ToListAsync(cancellationToken);
        UserClaimsSet.RemoveRange(claimsToRemove);
        try {
            await SaveChanges(cancellationToken);
        } catch (DbUpdateConcurrencyException) {
            return IdentityResult.Failed(ErrorDescriber.ConcurrencyFailure());
        }
        return IdentityResult.Success;
    }

    /// <inheritdoc/>
    public async Task<IList<Claim>> FindClaimsByTypeAsync(TUser user, string claimType, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        var claimsOfType = await UserClaimsSet.Where(x => x.ClaimType == claimType && x.UserId == user.Id).Select(x => new Claim(x.ClaimType!, x.ClaimValue!)).ToListAsync(cancellationToken);
        return claimsOfType;
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> ReplaceClaimAsync(TUser user, string claimType, string? claimValue, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        var claimsToReplace = await FindClaimsByTypeAsync(user, claimType, cancellationToken);
        if (string.IsNullOrWhiteSpace(claimValue)) {
            await RemoveClaimsAsync(user, claimsToReplace, cancellationToken);
        } else {
            var newClaim = new Claim(claimType, claimValue!);
            if (claimsToReplace.Count > 0) {
                if (claimsToReplace.Count == 1) {
                    await ReplaceClaimAsync(user, claimsToReplace.First(), newClaim, cancellationToken);
                } else {
                    await RemoveClaimsAsync(user, claimsToReplace, cancellationToken);
                    await AddClaimsAsync(user, [newClaim], cancellationToken);
                }
            } else {
                await AddClaimsAsync(user, [newClaim], cancellationToken);
            }
        }
        try {
            await SaveChanges(cancellationToken);
        } catch (DbUpdateException) {
            return IdentityResult.Failed();
        }
        return IdentityResult.Success;
    }

#if NET7_0_OR_GREATER
    [StringSyntax("Regex")]
#endif
    private const string Base64PicturePattern = "data:(?<ContentType>.+);base64,(?<Data>.+)";
#if NET7_0_OR_GREATER
    [GeneratedRegex(Base64PicturePattern)]
    private static partial Regex GetBase64PictureRegex();
#else
    private static readonly Regex _base64PictureRegex = new(Base64PicturePattern, RegexOptions.Compiled);
    private static Regex GetBase64PictureRegex() => _base64PictureRegex;
#endif

    /// <inheritdoc/>
    public async Task<IdentityResult> SetUserPictureAsync(TUser user, Stream inputStream, int sideSize = 256, double scale = 1, int translateX = 0, int translateY = 0, int viewPortSize = 256, CancellationToken cancellationToken = default) {
        (var outputStream, var contentType, var errors) = await SetUserPictureInternalAsync(inputStream, sideSize, scale, translateX, translateY, viewPortSize, cancellationToken);
        if (errors.Count > 0) {
            return IdentityResult.Failed([.. errors]);
        }
        if (StorePictureAsClaim) {
            var data = new StringBuilder();
            data.AppendFormat("data:{0};base64,", contentType!);
            data.Append(Convert.ToBase64String(outputStream!.ToArray()));

            var result = await ReplaceClaimAsync(user, IUserPictureStore<TUser>.PictureDataClaimType, data.ToString(), cancellationToken);
            return result;
        }
        var picture = await UserPictureSet.Where(x => x.UserId == user.Id).FirstOrDefaultAsync(cancellationToken);
        if (picture is not null) {
            UserPictureSet.Remove(picture);
        }
        picture = new() {
            UserId = user.Id,
            PictureKey = user.Id.ToSha256Hex(),
            ContentType = contentType,
            ContentLength = (int)outputStream!.Length,
            CreatedDate = DateTimeOffset.UtcNow,
            Data = outputStream.ToArray(),

        };
        UserPictureSet.Add(picture);
        try {
            await SaveChanges(cancellationToken);
        } catch (DbUpdateException) {
            return IdentityResult.Failed();
        }
        return IdentityResult.Success;

    }
    /// <inheritdoc/>
    public async Task<(Stream? Stream, string ContentType)> GetUserPictureAsync(TUser user, string? contentType = null, int? size = null, CancellationToken cancellationToken = default) {
        if (StorePictureAsClaim) {
            var claims = await FindClaimsByTypeAsync(user, IUserPictureStore<TUser>.PictureDataClaimType, cancellationToken);
            var avatarBinary = claims.FirstOrDefault();
            if (avatarBinary != null && !string.IsNullOrEmpty(avatarBinary.Value)) {
                var regex = GetBase64PictureRegex();
                var match = regex.Match(avatarBinary.Value);
                if (!match.Groups["ContentType"].Success) {
                    return (null, string.Empty);
                }
                if (!match.Groups["Data"].Success) {
                    return (null, string.Empty);
                }
                var base64 = match.Groups["Data"].Value;
                var savedContentType = match.Groups["ContentType"].Value;
                return await GetUserPictureInternalAsync(savedContentType, new MemoryStream(Convert.FromBase64String(base64)), contentType, size, cancellationToken);
            }
        } else {
            var picture = await UserPictureSet.Where(x => x.UserId == user.Id).FirstOrDefaultAsync(cancellationToken);
            if (picture is null) {  
                return (null, string.Empty); 
            }
            return await GetUserPictureInternalAsync(picture.ContentType, new MemoryStream(picture.Data), contentType, size, cancellationToken);
        }
        return (null, string.Empty);
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> ClearUserPictureAsync(TUser user, CancellationToken cancellationToken = default) {
        if (StorePictureAsClaim) {
            return await RemoveAllClaimsAsync(user, IUserPictureStore<TUser>.PictureDataClaimType, cancellationToken);
        }
        var pictures = await UserPictureSet.Where(x => x.UserId == user.Id).ToListAsync(cancellationToken);
        UserPictureSet.RemoveRange(pictures);
        try {
            await SaveChanges(cancellationToken);
        } catch (DbUpdateException) {
            return IdentityResult.Failed();
        }
        return IdentityResult.Success;
    }

    /// <inheritdoc/>
    public async Task<(Stream? Stream, string ContentType, bool Exists)> FindUserPictureByKeyAsync(string pictureKey, string? contentType = null, int? size = null, CancellationToken cancellationToken = default) {
        if (StorePictureAsClaim) {
            var user = await FindByIdAsync(pictureKey, cancellationToken);
            if (user is null) {
                return (Stream: null, ContentType: string.Empty, Exists: false);
            }
            var result = await GetUserPictureAsync(user, contentType, size, cancellationToken);
            return (result.Stream, result.ContentType, Exists: true);
        }
        bool isUserId = Guid.TryParse(pictureKey, out var _);

        var picture = isUserId ? await UserPictureSet.Where(x => x.UserId == pictureKey).FirstOrDefaultAsync(cancellationToken)
                                : await UserPictureSet.Where(x => x.PictureKey == pictureKey).FirstOrDefaultAsync(cancellationToken);
        if (picture is null) {
            return (Stream: null, ContentType: string.Empty, Exists: true);
        }
        (var outStream, var outType) = await GetUserPictureInternalAsync(picture.ContentType, new MemoryStream(picture.Data), contentType, size, cancellationToken);
        return (outStream, outType, Exists: true);
    }


    private static async Task<(MemoryStream? OutputStream, string ContentType, List<IdentityError> Errors)> SetUserPictureInternalAsync(Stream inputStream, int sideSize = 256, double scale = 1, int translateX = 0, int translateY = 0, int viewPortSize = 256, CancellationToken cancellationToken = default) {
        using var image = Image.Load(inputStream, out var format);
        // manipulate image resize to max side size.
        var maxSide = Math.Max(image.Width, image.Height);
        var errors = new List<IdentityError>();
        var contentType = "image/webp";
        if (scale < 1) {
            errors.Add(new IdentityError() { Code = nameof(scale), Description = "Scale cannot be less that 1." });
        }
        if (sideSize < 24) {
            errors.Add(new IdentityError() { Code = nameof(sideSize), Description = "Side size of the target imagage cannot be less that 24 pxels." });
        }
        if (errors.Count > 0) {
            return (OutputStream: null, ContentType: contentType, Errors: errors);
        }
        if (scale > 1) {
            // convert pan X and Y to my coord system.
            translateX = viewPortSize > 0 ? maxSide * translateX / viewPortSize : translateX;
            translateY = viewPortSize > 0 ? maxSide * translateY / viewPortSize : translateY;
            var cropWindow = new Size(Math.Max((int)(image.Width / scale), (int)(image.Height / scale)));
            var cropOptions = new Rectangle() {
                Size = cropWindow,
                X = (int)(((image.Width - cropWindow.Width) / 2d) - translateX),
                Y = (int)(((image.Height - cropWindow.Height) / 2d) - translateY),
            };

            var resizeOptions = new ResizeOptions() {
                Size = new Size(sideSize, sideSize),
            };
            try {
                image.Mutate(i => i.Crop(cropOptions).Resize(resizeOptions));
            } catch {
                errors.Add(new() { Code = "Crop", Description = "Scale and translate out of bounds." });
                return (OutputStream: null, ContentType: contentType, Errors: errors);
            }
        } else {
            var factor = (double)sideSize / maxSide;
            var resizeOptions = new ResizeOptions() {
                Size = new Size((int)(image.Width * factor), (int)(image.Height * factor)),
            };
            image.Mutate(i => i.Resize(resizeOptions));
        }

        var outputStream = new MemoryStream();
        await image.SaveAsWebpAsync(outputStream);
        outputStream.Seek(0, SeekOrigin.Begin);
        return (OutputStream: outputStream, ContentType: contentType, Errors: errors);
    }

    private static async Task<(Stream? Stream, string ContentType)> GetUserPictureInternalAsync(string originalContentType, MemoryStream originalStream, string? contentType = null, int? size = null, CancellationToken cancellationToken = default) {
        if (!size.HasValue && string.IsNullOrEmpty(contentType)) {
            return (Stream: originalStream, ContentType: originalContentType);
        }
        using var image = Image.Load(originalStream, out var format);
        var resizeOptions = new ResizeOptions() {
            Size = new Size(size ?? image.Width, size ?? image.Height),
            Mode = ResizeMode.Pad
        };
        image.Mutate(i => i.Resize(resizeOptions));
        var outputStream = new MemoryStream();
        contentType ??= format.DefaultMimeType;
        switch (contentType) {
            case "image/png":
                await image.SaveAsPngAsync(outputStream, cancellationToken);
                break;
            case "image/jpeg":
                await image.SaveAsJpegAsync(outputStream, cancellationToken);
                break;
            default:
                await image.SaveAsWebpAsync(outputStream, cancellationToken);
                break;
        }
        outputStream.Seek(0, SeekOrigin.Begin);
        return (Stream: outputStream, ContentType: contentType);
    }
}
