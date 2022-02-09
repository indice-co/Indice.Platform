using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Indice.AspNetCore.EmbeddedUI
{
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

    internal class SpaIndexFileInfo : IFileInfo
    {
        private readonly IFileInfo _template;
        private readonly SpaUIOptions _options;
        private long? _length;

        public SpaIndexFileInfo(IFileInfo template, SpaUIOptions options) {
            _template = template ?? throw new ArgumentNullException(nameof(template));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public bool Exists => true;

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

        public string PhysicalPath => null;
        public string Name => _template.Name;
        public DateTimeOffset LastModified => _template.LastModified;
        public bool IsDirectory => false;

        public Stream CreateReadStream() {
            using (var stream = _template.CreateReadStream()) {
                using (var streamReader = new StreamReader(stream)) {
                    var htmlBuilder = new StringBuilder(streamReader.ReadToEnd());
                    foreach (var argument in GetIndexArguments()) {
                        htmlBuilder.Replace(argument.Key, argument.Value);
                    }
                    return new MemoryStream(Encoding.UTF8.GetBytes(htmlBuilder.ToString()));
                }
            }
        }

        private IDictionary<string, string> GetIndexArguments() => new Dictionary<string, string>() {
            { "%(Authority)", _options.Authority.TrimEnd('/') },
            { "%(ClientId)", _options.ClientId },
            { "%(DocumentTitle)", _options.DocumentTitle },
            { "%(Host)", _options.Host.TrimEnd('/') },
            { "%(Path)", _options.Path.Trim('/') },
            { "%(HeadContent)", _options.HeadContent },
            { "%(Culture)", CultureInfo.CurrentCulture.TwoLetterISOLanguageName },
            { "%(ProductVersion)", _options.Version },
            { "%(Scopes)", _options.Scope },
            { "%(PostLogoutRedirectUri)", _options.PostLogoutRedirectUri }
        };
    }
}
