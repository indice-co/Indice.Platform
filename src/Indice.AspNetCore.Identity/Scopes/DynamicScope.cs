using System;
using System.Collections.Generic;
using IdentityServer4.Models;

namespace Indice.AspNetCore.Identity.Scopes
{
    /// <summary>
    /// Encapsulates a pattern along with the value (name)
    /// </summary>
    public class DynamicScope : Scope, ICloneable
    {
        /// <summary>
        /// Construct the <see cref="DynamicScope"/>
        /// </summary>
        public DynamicScope() {
        }

        /// <summary>
        /// Construct the <see cref="DynamicScope"/>
        /// </summary>
        /// <param name="name"></param>
        public DynamicScope(string name) : base(name) {
        }

        /// <summary>
        /// Construct the <see cref="DynamicScope"/>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="displayName"></param>
        public DynamicScope(string name, string displayName) : base(name, displayName) {
        }

        /// <summary>
        /// Construct the <see cref="DynamicScope"/>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="claimTypes"></param>
        public DynamicScope(string name, IEnumerable<string> claimTypes) : base(name, claimTypes) {
        }

        /// <summary>
        /// Construct the <see cref="DynamicScope"/>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="displayName"></param>
        /// <param name="claimTypes"></param>
        public DynamicScope(string name, string displayName, IEnumerable<string> claimTypes) : base(name, displayName, claimTypes) {
        }

        /// <summary>
        /// Regex or other pattern
        /// </summary>
        public string Pattern { get; set; }

        /// <summary>
        /// Strong customer authentication is required in order to consent to this scope.
        /// </summary>
        public bool RequiresSca { get; set; }

        /// <summary>
        /// Clone the <see cref="DynamicScope"/>
        /// </summary>
        /// <returns>A new instance</returns>
        public DynamicScope Clone() {
            return new DynamicScope {
                Name = Name,
                DisplayName = DisplayName,
                Description = Description,
                Emphasize = Emphasize,
                Pattern = Pattern,
                Required  = Required,
                UserClaims = UserClaims,
                ShowInDiscoveryDocument = ShowInDiscoveryDocument,
                RequiresSca = RequiresSca
            };
        }

        /// <summary>
        /// Clone the <see cref="DynamicScope"/>
        /// </summary>
        /// <returns>A new instance</returns>
        object ICloneable.Clone() {
            return Clone();
        }
    }
}
