using System;
using System.Text;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using UAParser;

namespace Indice.AspNetCore.Identity
{
    /// <summary>An implementation of <see cref="IRememberTwoFactorClientProvider{TUser}"/> where the client (browser) is persisted in a database.</summary>
    /// <typeparam name="TUser">The user entity.</typeparam>
    public class RememberTwoFactorClientDatabase<TUser> : IRememberTwoFactorClientProvider<TUser> where TUser : User
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ExtendedUserManager<TUser> _userManager;

        /// <summary>Creates a new instance of <see cref="RememberTwoFactorClientDatabase{TUser}"/> class.</summary>
        /// <param name="httpContextAccessor">Provides access to the current Microsoft.AspNetCore.Http.IHttpContextAccessor.HttpContext, if one is available.</param>
        /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public RememberTwoFactorClientDatabase(
            IHttpContextAccessor httpContextAccessor,
            ExtendedUserManager<TUser> userManager) {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        /// <inheritdoc />
        public async Task<bool> IsTwoFactorClientRememberedAsync(TUser user) {
            var request = _httpContextAccessor.HttpContext.Request;
            var containsDeviceId = request.Form.TryGetValue("DeviceId", out var deviceId);
            if (!containsDeviceId) {
                return false;
            }
            var device = await _userManager.GetDeviceByIdAsync(user, deviceId);
            return device is not null && device.RememberClientExpirationDate > DateTimeOffset.UtcNow;
        }

        /// <inheritdoc />
        public async Task RememberTwoFactorClientAsync(TUser user) {
            var request = _httpContextAccessor.HttpContext.Request;
            var containsDeviceId = request.Form.TryGetValue("DeviceId", out var deviceId);
            if (!containsDeviceId) {
                return;
            }
            var device = await _userManager.GetDeviceByIdAsync(user, deviceId);
            if (device is not null) {
                device.RememberClientExpirationDate = DateTimeOffset.UtcNow.AddDays(90);
                await _userManager.UpdateDeviceAsync(user, device);
                return;
            }
            var userAgent = request.Headers[HeaderNames.UserAgent];
            ClientInfo clientInfo = null;
            if (!string.IsNullOrWhiteSpace(userAgent)) {
                var uaParser = Parser.GetDefault();
                clientInfo = uaParser.Parse(userAgent);
            }
            var osInfo = FormatOsInfo(clientInfo?.OS);
            var name = $"{FormatUserAgentInfo(clientInfo?.UA)} on {osInfo}".Trim();
            device = new UserDevice {
                ClientType = UserDeviceType.Browser,
                DateCreated = DateTimeOffset.UtcNow,
                DeviceId = deviceId,
                Model = FormatDeviceInfo(clientInfo?.Device),
                Name = name == string.Empty ? null : name,
                OsVersion = osInfo,
                Platform = DevicePlatform.None,
                RememberClientExpirationDate = DateTimeOffset.UtcNow.AddDays(90),
                User = user,
                UserId = user.Id
            };
            await _userManager.CreateDeviceAsync(user, device);
        }

        private static string FormatUserAgentInfo(UserAgent userAgent) {
            if (userAgent is null) {
                return default;
            }
            var stringBuilder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(userAgent.Family)) {
                stringBuilder.Append(userAgent.Family);
            }
            if (!string.IsNullOrWhiteSpace(userAgent.Major)) {
                stringBuilder.Append($" {userAgent.Major}");
            }
            if (!string.IsNullOrWhiteSpace(userAgent.Minor)) {
                stringBuilder.Append($".{userAgent.Minor}");
            }
            if (!string.IsNullOrWhiteSpace(userAgent.Patch)) {
                stringBuilder.Append($".{userAgent.Patch}");
            }
            var userAgentInfo = stringBuilder.ToString().Trim();
            return userAgentInfo == string.Empty ? null : userAgentInfo;
        }

        private static string FormatOsInfo(OS os) {
            if (os is null) {
                return default;
            }
            var stringBuilder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(os.Family)) {
                stringBuilder.Append(os.Family);
            }
            if (!string.IsNullOrWhiteSpace(os.Major)) {
                stringBuilder.Append($" {os.Major}");
            }
            if (!string.IsNullOrWhiteSpace(os.Minor)) {
                stringBuilder.Append($".{os.Minor}");
            }
            if (!string.IsNullOrWhiteSpace(os.Patch)) {
                stringBuilder.Append($".{os.Patch}");
            }
            if (!string.IsNullOrWhiteSpace(os.PatchMinor)) {
                stringBuilder.Append($".{os.PatchMinor}");
            }
            var osInfo = stringBuilder.ToString().Trim();
            return osInfo == string.Empty ? null : osInfo;
        }

        private static string FormatDeviceInfo(Device device) {
            if (device is null) {
                return default;
            }
            var stringBuilder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(device.Family)) {
                stringBuilder.Append(device.Family);
            }
            if (!string.IsNullOrWhiteSpace(device.Brand)) {
                stringBuilder.Append($" {device.Brand}");
            }
            if (!string.IsNullOrWhiteSpace(device.Model)) {
                stringBuilder.Append($" {device.Model}");
            }
            var deviceInfo = stringBuilder.ToString().Trim();
            return deviceInfo == string.Empty ? null : deviceInfo;
        }
    }
}
