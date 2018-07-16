namespace Indice.Services
{
    /// <summary>
    /// Specifies methods for encrypting and decrypting data by using the Data Protection API.
    /// </summary>
    /// <typeparam name="T">The type of the data to encrypt or decrypt.</typeparam>
    public interface IDataProtectionEncryptor<T>
    {
        /// <summary>
        /// Cryptographically protects a piece of plaintext data.
        /// </summary>
        /// <param name="object">The data to encrypt.</param>
        string Encrypt(T @object);

        /// <summary>
        /// Cryptographically unprotects a piece of protected data.
        /// </summary>
        /// <param name="encryptedText">A piece of plaintext data to decrypt.</param>
        /// <param name="object">Returns an object of the specified type that was decrypted.</param>
        bool TryDecrypt(string encryptedText, out T @object);
    }
}
