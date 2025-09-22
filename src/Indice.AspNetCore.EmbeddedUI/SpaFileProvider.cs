using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileSystemGlobbing;
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
            return new SpaIndexFileInfo(_inner.GetFileInfo("index.html"), _options, _inner );
        }
        return _inner.GetFileInfo(subpath);
    }

    public IChangeToken Watch(string filter) => _inner.Watch(filter);
}

/// <summary>Represents the starting point file for a SPA (index.html) in the given file provider.</summary>
internal partial class SpaIndexFileInfo : IFileInfo
{
    private readonly IFileInfo _fileInfo;
    private readonly SpaUIOptions _options;
    private readonly EmbeddedFileProvider _embeddedFileProvider;
    private long? _length;

    /// <summary>Creates a new instance of <see cref="SpaIndexFileInfo"/>.</summary>
    /// <param name="fileInfo">Represents a file in the given file provider.</param>
    /// <param name="options">Options for configuring <see cref="SpaUIMiddleware{TOptions}"/> middleware.</param>
    /// <param name="embeddedFileProvider">The embedded file provider.</param>
    public SpaIndexFileInfo(IFileInfo fileInfo, SpaUIOptions options, EmbeddedFileProvider embeddedFileProvider) {
        _fileInfo = fileInfo ?? throw new ArgumentNullException(nameof(fileInfo));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _embeddedFileProvider = embeddedFileProvider ?? throw new ArgumentNullException(nameof(embeddedFileProvider));
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
                // Improved regex: capture the whole element (<link ...> or <script ...></script>) as "element",
                // the tag name as "tag" and the href/src value (that contains a glob *) as "path".
                // It supports self-closing link tags and script tags with inner content.
                var globbingPathRegex = HrefSrcGlobbingRegex();
                var htmlString = htmlBuilder.ToString();
                htmlString = globbingPathRegex.Replace(htmlString, new MatchEvaluator((match) => {
                    var path = match.Groups["path"].Value;
                    var element = match.Groups["element"].Value;
                    // At this point you have:
                    // - element: the entire <link ...> or <script ...>...</script> text
                    // - path: the globbing path value (contains '*')
                    //
                    // TODO: enumerate matching embedded resources/files for 'path' and
                    // produce one element per found file, adjusting href/src attribute to point to each file.
                    // For now, return the original element to keep behavior unchanged and compilable.
                    var results = GetFileNamesFromEmbeddedResource(Path.GetFileName(path));
                    var sb = new StringBuilder();
                    foreach (var item in results) {
                        sb.AppendLine(element.Replace(Path.GetFileName(path), item));
                    }
                    if (sb.Length != 0) {
                        return sb.ToString();
                    }
                    return element;
                }));

                return new MemoryStream(Encoding.UTF8.GetBytes(htmlString));
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
            ["%(Culture)"] = CultureInfo.InvariantCulture.TwoLetterISOLanguageName.Equals(CultureInfo.CurrentCulture.TwoLetterISOLanguageName) ? "el" : CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
            ["%(ProductVersion)"] = _options.Version,
            ["%(Scopes)"] = _options.Scope,
            ["%(PostLogoutRedirectUri)"] = _options.PostLogoutRedirectUri?.Trim('/') ?? string.Empty,
            ["%(TenantId)"] = _options.TenantId ?? string.Empty
        };
        _options.ConfigureIndexParameters?.Invoke(arguments);
        return arguments;
    }
    
    private IEnumerable<string> GetFileNamesFromEmbeddedResource(string resourceName) {
        var matcher = new Matcher().AddInclude(resourceName);
        var resourceNames = _embeddedFileProvider.GetDirectoryContents("/");
        var result = matcher.Execute(new InMemoryDirectoryInfo("/", resourceNames.Select(x => x.Name)));
        return result.Files.Select(x => x.Path);
    }

    [GeneratedRegex(@"(?<element><(?<tag>link|script)\b[^>]*?(?:href|src)=['""](?<path>[^'""]*\*[^'""]*)['""][^>]*?(?:>(?<inner>.*?)</\k<tag>\s*>|/?>))", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline)]
    internal static partial Regex HrefSrcGlobbingRegex();
}
