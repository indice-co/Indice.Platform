// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Indice.Configuration;

namespace System.Security
{
    /// <summary>
    /// TOTP: Time-Based One-Time Password Algorithm. More info: https://tools.ietf.org/html/rfc6238
    /// </summary>
    public class Rfc6238AuthenticationService
    {
        private readonly Rfc6238AuthenticationServiceOptions _options;
        private readonly DateTime _unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private readonly TimeSpan _timestep = TimeSpan.FromMinutes(0.5d);
        private readonly Encoding _encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        /// <summary>
        /// Creates a new instance of <see cref="Rfc6238AuthenticationService"/>.
        /// </summary>
        public Rfc6238AuthenticationService(Rfc6238AuthenticationServiceOptions options) {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (_options.TimeStep.HasValue) {
                _timestep = TimeSpan.FromMinutes(_options.TimeStep.Value);
            }
        }

        /// <summary>
        /// Generate the code.
        /// </summary>
        /// <param name="securityToken">A security code. This should be a secret.</param>
        /// <param name="modifier">Entropy.</param>
        /// <returns>Returns the generated code.</returns>
        public int GenerateCode(byte[] securityToken, string modifier = null) {
            if (securityToken == null) {
                throw new ArgumentNullException(nameof(securityToken));
            }
            var currentTimeStep = GetCurrentTimeStepNumber();
            using (var hashAlgorithm = new HMACSHA1(securityToken)) {
                return ComputeTotp(hashAlgorithm, currentTimeStep, modifier);
            }
        }

        /// <summary>
        /// Validates the <paramref name="code"/>.
        /// </summary>
        /// <param name="securityToken">A security code. This should be a secret.</param>
        /// <param name="code">The code.</param>
        /// <param name="modifier">Entropy.</param>
        /// <returns>Returns true if code is valid, otherwise false.</returns>
        public bool ValidateCode(byte[] securityToken, int code, string modifier = null) {
            if (securityToken == null) {
                throw new ArgumentNullException(nameof(securityToken));
            }
            var currentTimeStep = GetCurrentTimeStepNumber();
            using (var hashAlgorithm = new HMACSHA1(securityToken)) {
                for (var i = -2; i <= 2; i++) {
                    var computedTotp = ComputeTotp(hashAlgorithm, (ulong)((long)currentTimeStep + i), modifier);
                    if (computedTotp == code) {
                        return true;
                    }
                }
            }
            return false;
        }

        private int ComputeTotp(HashAlgorithm hashAlgorithm, ulong timestepNumber, string modifier) {
            // # of 0's = length of pin.
            const int Mod = 1000000;
            // See https://tools.ietf.org/html/rfc4226
            // We can add an optional modifier.
            var timestepAsBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((long)timestepNumber));
            var hash = hashAlgorithm.ComputeHash(ApplyModifier(timestepAsBytes, modifier));
            // Generate DT string.
            var offset = hash[hash.Length - 1] & 0xf;
            Debug.Assert(offset + 4 < hash.Length);
            var binaryCode = (hash[offset] & 0x7f) << 24 | (hash[offset + 1] & 0xff) << 16 | (hash[offset + 2] & 0xff) << 8 | (hash[offset + 3] & 0xff);
            return binaryCode % Mod;
        }

        private byte[] ApplyModifier(byte[] input, string modifier) {
            if (string.IsNullOrEmpty(modifier)) {
                return input;
            }
            var modifierBytes = _encoding.GetBytes(modifier);
            var combined = new byte[checked(input.Length + modifierBytes.Length)];
            Buffer.BlockCopy(input, 0, combined, 0, input.Length);
            Buffer.BlockCopy(modifierBytes, 0, combined, input.Length, modifierBytes.Length);
            return combined;
        }

        // More info: https://tools.ietf.org/html/rfc6238#section-4
        private ulong GetCurrentTimeStepNumber() {
            var delta = DateTime.UtcNow - _unixEpoch;
            return (ulong)(delta.Ticks / _timestep.Ticks);
        }
    }
}