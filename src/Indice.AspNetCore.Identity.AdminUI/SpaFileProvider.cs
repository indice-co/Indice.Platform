using System;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Indice.AspNetCore.Identity.AdminUI
{
    /// <summary>
    /// An enhanced <see cref="EmbeddedFileProvider"/> used to serve a SPA.
    /// </summary>
    public class SpaFileProvider : IFileProvider
    {
        private readonly EmbeddedFileProvider _inner;

        /// <summary>
        /// Creates a new instance of <see cref="SpaFileProvider"/>.
        /// </summary>
        /// <param name="inner">Looks up files using embedded resources in the specified assembly. This file provider is case sensitive.</param>
        public SpaFileProvider(EmbeddedFileProvider inner) {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        /// <inheritdoc />
        public IDirectoryContents GetDirectoryContents(string subpath) => _inner.GetDirectoryContents(subpath);

        /// <inheritdoc />
        public IFileInfo GetFileInfo(string subpath) {
            var fileInfo = _inner.GetFileInfo(subpath);
            return fileInfo;
        }

        /// <inheritdoc />
        public IChangeToken Watch(string filter) => _inner.Watch(filter);
    }
}
