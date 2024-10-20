using System.Runtime.Intrinsics.Arm;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Bogus.DataSets;
using IdentityModel;
using IdentityServer4.EntityFramework.Entities;
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
public class ExtendedUserStore<TContext, TUser, TRole> : UserStore<TUser, TRole, TContext>, IExtendedUserStore<TUser>, IUserDeviceStore<TUser>, IUserPictureStore<TUser>
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
            }
        }
        try {
            await SaveChanges(cancellationToken);
        } catch (DbUpdateException) {
            return IdentityResult.Failed();
        }
        return IdentityResult.Success;
    }

    private static bool _SaveToClaim = false;
    /// <inheritdoc/>
    public async Task<IdentityResult> SetUserPictureAsync(TUser user, Stream inputStream, int sideSize = 256, CancellationToken cancellationToken = default) {
        using var image = Image.Load(inputStream, out var format);
        // manipulate image resize to max side size.
        var factor = (double)sideSize / Math.Max(image.Width, image.Height);
        var resizeOptions = new ResizeOptions() {
            Size = new Size((int)(image.Width * factor), (int)(image.Height * factor))
        };
        image.Mutate(i => i.Resize(resizeOptions));

        var outputStream = new MemoryStream();
        await image.SaveAsWebpAsync(outputStream);
        outputStream.Seek(0, SeekOrigin.Begin);
        var contentType = "image/webp";
        if (_SaveToClaim) {
            var data = new StringBuilder();
            data.AppendFormat("data:{0};base64,", contentType);
            data.Append(Convert.ToBase64String(outputStream.ToArray()));
            return await ReplaceClaimAsync(user, IUserPictureStore<TUser>.PictureDataClaimType, data.ToString(), cancellationToken);
        }

        var picture = await UserPictureSet.Where(x => x.UserId == user.Id).FirstOrDefaultAsync(cancellationToken: cancellationToken);
        if (picture is not null) {
            UserPictureSet.Remove(picture);
        }
        picture = new() {
            UserId = user.Id,
            PictureKey = user.Id.ToSha256(),
            ContentType = contentType,
            ContentLength = (int)outputStream.Length,
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
    public Task<IdentityResult> ClearUserPictureAsync(TUser user, CancellationToken cancellationToken = default) {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<(Stream? Stream, string ContentType)> GetUserPictureAsync(TUser user, string? contentType = null, int? size = null, CancellationToken cancellationToken = default) {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<bool> UserPictureExistsAsync(TUser user, CancellationToken cancellationToken = default) {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<(Stream? Stream, string ContentType)> FindUserPictureByKeyAsync(string pictureKey, string? contentType = null, int? size = null, CancellationToken cancellationToken = default) {
        throw new NotImplementedException();
    }
}
