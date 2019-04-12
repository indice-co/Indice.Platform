using System;
using Microsoft.AspNetCore.DataProtection;
using Newtonsoft.Json;

namespace Indice.Services
{
    /// <summary>
    /// Contains methods for encrypting and decrypting data by using the Data Protection API.
    /// </summary>
    /// <typeparam name="T">The type of the data to encrypt or decrypt.</typeparam>
    public class DataProtectionEncryptor<T> : IDataProtectionEncryptor<T> where T : new()
    {
        private readonly IDataProtector _protector;

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="provider">An interface that can be used to create <see cref="IDataProtector"/> instances.</param>
        public DataProtectionEncryptor(IDataProtectionProvider provider) {
            if (provider == null) {
                throw new ArgumentNullException(nameof(provider));
            }
            _protector = provider.CreateProtector(typeof(T).FullName);
        }

        /// <summary>
        /// Cryptographically protects a piece of plaintext data.
        /// </summary>
        /// <param name="object">A piece of plaintext data to protect.</param>
        public string Encrypt(T @object) {
            var json = JsonConvert.SerializeObject(@object);
            return _protector.Protect(json);
        }

        /// <summary>
        /// Cryptographically unprotects a piece of protected data.
        /// </summary>
        /// <param name="encryptedText">A piece of plaintext data to decrypt.</param>
        /// <param name="object">Returns an object of the specified type that was decrypted.</param>
        /// <returns>Return true if the decryption process completes successfully.</returns>
        public bool TryDecrypt(string encryptedText, out T @object) {
            try {
                var decryptedText = _protector.Unprotect(encryptedText);
                @object = JsonConvert.DeserializeObject<T>(decryptedText);
                return true;
            } catch (Exception) {
                @object = default;
                return false;
            }
        }
    }
}
