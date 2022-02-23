using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Indice.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Indice.AspNetCore.Middleware
{

    /// <summary>
    /// Provides programmatic configuration for the <see cref="ClientIpRestrictionMiddleware"/>.
    /// </summary>
    public class ClientIpRestrictionOptions
    {
        private const string DEFAULT_CONFIG_SECTION_KEY = "IpRestrictions";

        /// <summary>
        /// Default Configuration key
        /// </summary>
        internal string ConfigurationSectionName { get; private set; } = DEFAULT_CONFIG_SECTION_KEY;

        /// <summary>
        /// Paths that are exluded from <see cref="Mappings"/>, optionally based on provided HTTP method.
        /// </summary>
        internal Dictionary<string, string> IgnoredPaths { get; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        /// <summary>
        /// Map of route paths and client ips that will excluded from any restrictions.
        /// </summary>
        internal Dictionary<string, string> Mappings { get; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        /// <summary>
        /// Name value pair for lists of safe ips (in byte array format) and their friendly names.
        /// </summary>
        internal Dictionary<string, byte[][]> IpAddressLists { get; } = new Dictionary<string, byte[][]>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// Status code to use when a client ip is dened access. Defaults to <see cref="HttpStatusCode.Forbidden"/>
        /// </summary>
        /// <remarks>For example this could be a <strong>404</strong> Not found as well as a <strong>403</strong> Forbidden.</remarks>
        public HttpStatusCode HttpStatusCode { get; set; } = HttpStatusCode.Forbidden;

        /// <summary>
        /// Toggles feature. Defaults to false
        /// </summary>
        public bool Disabled { get; set; }

        /// <summary>
        /// Adds new list of ips under a given name.
        /// </summary>
        /// <param name="name">The list name.</param>
        /// <param name="ipAddresses">A semicolon delimited string of ips.</param>
        public ClientIpRestrictionOptions AddIpAddressList(string name, string ipAddresses) {
            var ips = ipAddresses.Split(';');
            var ipsBytes = new byte[ips.Length][];
            for (var i = 0; i < ips.Length; i++) {
                ipsBytes[i] = IPAddress.Parse(ips[i]).GetAddressBytes();
            }
            if (!IpAddressLists.ContainsKey(name)) {
                IpAddressLists.Add(name, ipsBytes);
            }
            return this;
        }

        /// <summary>
        /// Adds a new map entry to the dictionary of mappings. This will be picked up by the <see cref="ClientIpRestrictionMiddleware"/> in order to determine which ips are exempted from the restrictions.
        /// </summary>
        /// <param name="path">The path to map.</param>
        /// <param name="ipAddressesOrListName">An ip list name to use or a semicolon delimited string of ips.</param>
        public ClientIpRestrictionOptions MapPath(PathString path, string ipAddressesOrListName) {
            if (path.HasValue && path.Value.EndsWith("/", StringComparison.Ordinal)) {
                throw new ArgumentException("The path must not end with a '/'", nameof(path));
            }
            if (path.HasValue) {
                var listName = ipAddressesOrListName;
                bool isIpList = ipAddressesOrListName.Contains(';') || IPAddress.TryParse(ipAddressesOrListName, out var _);
                if (isIpList) {
                    listName = ipAddressesOrListName.GetHashCode().ToString();
                    AddIpAddressList(listName, ipAddressesOrListName);
                }
                if (Mappings.ContainsKey(path.Value)) {
                    Mappings[path.Value] = listName;
                } else {
                    Mappings.Add(path.Value, listName);
                }
            }
            return this;
        }

        /// <summary>
        /// Excludes a mapped path, optionally based on the given HTTP method. If HTTP method is not specified, every request to this path will not be used by <see cref="ClientIpRestrictionMiddleware"/>.
        /// </summary>
        /// <param name="pathString">The path to exclude.</param>
        /// <param name="httpMethods">The HTTP methods to exclude for the given path.</param>
        public ClientIpRestrictionOptions IgnorePath(PathString pathString, params string[] httpMethods) {
            if (pathString == null) {
                throw new ArgumentNullException(nameof(pathString), "Cannot ignore a null path.");
            }
            var path = pathString.Value.EnsureLeadingSlash().ToTemplatedDynamicPath();
            // No HTTP methods specified, so exclude just the path (implies that all HTTP methods will be excluded for this path).
            if (httpMethods?.Length == 0) {
                IgnoredPaths.Add(path, "*");
                return this;
            }
            // Validate HTTP method.
            // There are more of course, but this seems enough for our needs.
            foreach (var method in httpMethods) {
                var isValidHttpMethod = HttpMethods.IsGet(method) || HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsDelete(method) || HttpMethods.IsPatch(method);
                if (!isValidHttpMethod) {
                    throw new ArgumentException($"HTTP method {method} is not valid.");
                }
            }
            if (!IgnoredPaths.ContainsKey(path)) {
                IgnoredPaths.Add(path, string.Join('|', httpMethods));
            } else {
                var methods = IgnoredPaths[path].Split('|').Union(httpMethods);
                IgnoredPaths[path] = string.Join('|', methods);
            }
            return this;
        }

        /// <summary>
        /// Tries to find a matching path.
        /// </summary>
        /// <param name="path">The path to match.</param>
        /// <param name="httpMethod">The HTTP method of the specified path.</param>
        /// <param name="ipSafeList">The ips to be whitelisted.</param>
        public bool TryMatch(PathString path, string httpMethod, out byte[][] ipSafeList) {
            ipSafeList = null;
            if (Mappings.ContainsKey(path) && !InternalStringExtensions.IsIgnoredPath(IgnoredPaths, path, httpMethod)) {
                ipSafeList = IpAddressLists[Mappings[path]];
                return true;
            }
            var results = Mappings.Where(x => path.StartsWithSegments(x.Key));
            if (results.Any() && !InternalStringExtensions.IsIgnoredPath(IgnoredPaths, path, httpMethod)) {
                var iplistName = results.OrderByDescending(x => x.Key.Length).First().Value;
                ipSafeList = IpAddressLists[iplistName];
                return true;
            }
            return false;
        }

        /// <summary>
        /// Tries to find a matching path.
        /// </summary>
        /// <param name="httpContext">The path to match.</param>
        /// <param name="ipSafeList">The ips to be whitelisted for the current path.</param>
        public bool TryMatch(HttpContext httpContext, out byte[][] ipSafeList) {
            var path = httpContext.Request.Path;
            var httpMethod = httpContext.Request.Method;
            var isMatch = TryMatch(path, httpMethod, out var ipsInner);
            ipSafeList = ipsInner;
            return isMatch;
        }

        /// <summary>
        /// Marks options to be loaded from from <see cref="IConfiguration"/>.
        /// </summary>
        /// <param name="sectionName">If null defaults to <strong>IPRestrictions</strong></param>
        /// <returns>Self</returns>
        public ClientIpRestrictionOptions LoadFromConfiguration(string sectionName = null) {
            ConfigurationSectionName = sectionName ?? DEFAULT_CONFIG_SECTION_KEY;
            return this;
        }
    }

    internal class ClientIpRestrictionRule
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string IpAddresses { get; set; }
    }

    internal class ClientIpRestrictionIgnore
    {
        public string Path { get; set; }
        public string HttpMethods { get; set; }
    }
}
