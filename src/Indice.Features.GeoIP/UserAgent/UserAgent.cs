using System.Reflection;
using System.Text;
using Indice.Types;
using Microsoft.Net.Http.Headers;
using UAParser;

namespace Indice.Features.GeoIP.UserAgent;

/// <summary>Models a user agent (browser) type, extracting various useful information.</summary>
public class UserAgent
{
    private static readonly Lazy<Parser> LazyParser = new(CreateParser, LazyThreadSafetyMode.ExecutionAndPublication);
    private static Parser Parser => LazyParser.Value;
    /// <summary>Creates a new instance of <see cref="UserAgent"/> class, accepting the <see cref="HeaderNames.UserAgent"/> header value as parameter.</summary>
    /// <param name="userAgentHeader">The <see cref="HeaderNames.UserAgent"/> header value.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public UserAgent(string userAgentHeader) {
        ArgumentNullException.ThrowIfNull(userAgentHeader);
        HeaderValue = userAgentHeader;
        var clientInfo = Parser.Parse(userAgentHeader);
        Os = FormatOsInfo(clientInfo?.OS);
        DisplayName = $"{FormatUserAgentInfo(clientInfo?.UA)} on {Os}".Trim();
        DevicePlatform = DecideDevicePlatform(Os);
        DeviceModel = FormatDeviceInfo(clientInfo?.Device);
    }
    /// <summary>
    /// Creates and initializes a UAParser <see cref="Parser"/> instance from the embedded <c>regexes.yaml</c> resource.
    /// Loads the resource from the executing assembly. If the resource cannot be found, falls back to the default parser.
    /// </summary>
    /// <returns>A configured <see cref="Parser"/> instance.</returns>
    private static Parser CreateParser() {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "Indice.Features.GeoIP.UserAgent.regexes.yaml";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            return Parser.GetDefault();

        using var reader = new StreamReader(stream, Encoding.UTF8);
        var yaml = reader.ReadToEnd();

        return Parser.FromYaml(yaml);
    }

    /// <summary>The device model.</summary>
    public string? DeviceModel { get; set; }
    /// <summary>The device platform.</summary>
    public DevicePlatform DevicePlatform { get; set; }
    /// <summary>Browser display name.</summary>
    public string DisplayName { get; }
    /// <summary>The operating system name.</summary>
    public string? Os { get; }
    /// <summary>The raw value of the header.</summary>
    public string HeaderValue { get; }

    private static string? FormatUserAgentInfo(UAParser.UserAgent? userAgent) {
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
        return userAgentInfo == string.Empty ? default : userAgentInfo;
    }

    private static string? FormatOsInfo(OS? os) {
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
        return osInfo == string.Empty ? default : osInfo;
    }

    private static string? FormatDeviceInfo(Device? device) {
        if (device is null) {
            return default;
        }
        var stringBuilder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(device.Brand)) {
            stringBuilder.Append($" {device.Brand}");
        }
        if (!string.IsNullOrWhiteSpace(device.Model)) {
            stringBuilder.Append($" {device.Model}");
        }
        var deviceInfo = stringBuilder.ToString().Trim();
        return deviceInfo == string.Empty ? default : deviceInfo;
    }

    private static DevicePlatform DecideDevicePlatform(string? osInfo) {
        var devicePlatform = DevicePlatform.None;
        switch (osInfo) {
            case string x when x.Contains("iPhone", StringComparison.OrdinalIgnoreCase):
                devicePlatform = DevicePlatform.iOS;
                break;
            case string x when x.Contains("iOS", StringComparison.OrdinalIgnoreCase):
                devicePlatform = DevicePlatform.iOS;
                break;
            case string x when x.Contains("Android", StringComparison.OrdinalIgnoreCase):
                devicePlatform = DevicePlatform.Android;
                break;
            case string x when x.Contains("Windows", StringComparison.OrdinalIgnoreCase):
                devicePlatform = DevicePlatform.Windows;
                break;
            case string x when x.Contains("Linux", StringComparison.OrdinalIgnoreCase):
                devicePlatform = DevicePlatform.Linux;
                break;
            case string x when x.Contains("Ubuntu", StringComparison.OrdinalIgnoreCase):
                devicePlatform = DevicePlatform.Linux;
                break;
            case string x when x.Contains("Mac", StringComparison.OrdinalIgnoreCase):
                devicePlatform = DevicePlatform.MacOS;
                break;
        }
        return devicePlatform;
    }
}
