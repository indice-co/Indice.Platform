using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace Microsoft.Extensions.FileProviders
{
    /// <summary>
    /// Extension on the <see cref="IFileInfo"/>. Usefull when dealing with file providers and the static file middleware
    /// </summary>
    public static class IFileInfoExtensions
    {
        /// <summary>
        /// Generates Fingerprints 
        /// </summary>
        /// <typeparam name="THashAlgorithm">The hashing algorithm to use</typeparam>
        /// <param name="file">Input file</param>
        /// <param name="format">The string represenation for the byte array output</param>
        /// <returns></returns>
        public static string ComputeHash<THashAlgorithm>(this IFileInfo file, string format = "x2")
            where THashAlgorithm : HashAlgorithm, new() {
            // nothing to compute
            if (file?.Exists != true || file.IsDirectory || file.Length == 0) {
                return null;
            }

            using (var hasher = new THashAlgorithm())
            using (var stream = file.CreateReadStream()) {

                byte[] bytes = hasher.ComputeHash(stream);
                stream.Close();

                return bytes.Aggregate(
                    new StringBuilder(bytes.Length * 2),
                    (hash, value) => hash.Append(value.ToString(format))
                ).ToString();
            }
        }

    }
}
