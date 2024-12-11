using System.Globalization;
using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Indice.AspNetCore.EmbeddedUI;

internal class SpaFileProvider : IFileProvider
{
    private readonly EmbeddedFileProvider _inner;
    private readonly SpaUIOptions _options;

    public SpaFileProvider(EmbeddedFileProvider inner, SpaUIOptions options) {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public IDirectoryContents GetDirectoryContents(string subpath) => _inner.GetDirectoryContents(subpath);

    public IFileInfo GetFileInfo(string subpath) {
        if (subpath.Equals("/index.html", StringComparison.OrdinalIgnoreCase)) {
            return new SpaIndexFileInfo(_inner.GetFileInfo("index.html"), _options);
        }
        return _inner.GetFileInfo(subpath);
    }

    public IChangeToken Watch(string filter) => _inner.Watch(filter);
}

/// <summary>Represents the starting point file for a SPA (index.html) in the given file provider.</summary>
internal class SpaIndexFileInfo : IFileInfo
{
    private readonly IFileInfo _fileInfo;
    private readonly SpaUIOptions _options;
    private long? _length;

    /// <summary>Creates a new instance of <see cref="SpaIndexFileInfo"/>.</summary>
    /// <param name="fileInfo">Represents a file in the given file provider.</param>
    /// <param name="options">Options for configuring <see cref="SpaUIMiddleware{TOptions}"/> middleware.</param>
    public SpaIndexFileInfo(IFileInfo fileInfo, SpaUIOptions options) {
        _fileInfo = fileInfo ?? throw new ArgumentNullException(nameof(fileInfo));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public bool Exists => true;

    /// <inheritdoc />
    public long Length {
        get {
            if (!_length.HasValue) {
                using (var stream = CreateReadStream()) {
                    _length = stream.Length;
                }
            }
            return _length.Value;
        }
    }

    /// <inheritdoc />
    public string? PhysicalPath => null;
    /// <inheritdoc />
    public string Name => _fileInfo.Name;
    /// <inheritdoc />
    public DateTimeOffset LastModified => _fileInfo.LastModified;
    /// <inheritdoc />
    public bool IsDirectory => false;

    /// <inheritdoc />
    public Stream CreateReadStream() {
        using (var stream = _fileInfo.CreateReadStream()) {
            using (var streamReader = new StreamReader(stream)) {
                var htmlBuilder = new StringBuilder(streamReader.ReadToEnd());
                foreach (var argument in GetIndexArguments()) {
                    htmlBuilder.Replace(argument.Key, argument.Value);
                }
                return new MemoryStream(Encoding.UTF8.GetBytes(htmlBuilder.ToString()));
            }
        }
    }

    /// <summary>Creates a <see cref="Dictionary{TKey, TValue}"/> that is used to replace options in the index.html file.</summary>
    private IDictionary<string, string?> GetIndexArguments() {
        var arguments = new Dictionary<string, string?>() {
            ["%(Authority)"] = _options.Authority!.TrimEnd('/'),
            ["%(ClientId)"] = _options.ClientId,
            ["%(DocumentTitle)"] = _options.DocumentTitle,
            ["%(Host)"] = _options.Host!.TrimEnd('/'),
            ["%(ApiBase)"] = _options.ApiBase.TrimEnd('/'),
            ["%(Path)"] = _options.Path!.TrimEnd('/'),
            ["%(HeadContent)"] = _options.HeadContent,
            ["%(Culture)"] = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
            ["%(ProductVersion)"] = _options.Version,
            ["%(Scopes)"] = _options.Scope,
            ["%(PostLogoutRedirectUri)"] = _options.PostLogoutRedirectUri?.Trim('/') ?? string.Empty,
            ["%(TenantId)"] = _options.TenantId ?? string.Empty
        };
        _options.ConfigureIndexParameters?.Invoke(arguments);
        return arguments;
    }
}
