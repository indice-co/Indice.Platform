using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Identity.Services
{

    //https://stackoverflow.com/questions/868482/simulating-mysqls-password-encryption-using-net-or-ms-sql
    /// <summary>
    /// According to MySQL documentation, the algorithm is a double SHA1 hash. When examining the MySQL source code, you find a function called make_scrambled_password() 
    /// Compatible to mysql password() function for versions greater than v4.1.1
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public class MySqlPasswordHasher<TUser> : PasswordHasher<TUser> where TUser : class
    {
        /// <summary>
        /// constructs the PasswordHasher
        /// </summary>
        /// <param name="optionsAccessor"></param>
        public MySqlPasswordHasher(IOptions<PasswordHasherOptions> optionsAccessor = null) : base(optionsAccessor) {
        }

        /// <summary>
        /// When a password is provided that you need to compare against a hashed version, the <see cref="PasswordHasher{TUser}" /> needs to know which format was used to hash the password. 
        /// To do this, it preppends a single byte to the hash before storing it in the database (Base64 encoded). 
        /// When a password needs to be verified, the hasher checks the first byte, and uses the appropriate algorithm to hash the provided password.
        /// This method extends the functionality to account for mysql password hashes.
        /// </summary>
        /// <param name="user">The user</param>
        /// <param name="hashedPassword">The stored hash for the current password</param>
        /// <param name="providedPassword">The provided password to check</param>
        /// <returns>A result. Will always be RehashNeeded in case of successful mysql match</returns>
        public override PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword) {
            switch (hashedPassword[0]) {
                case '*': // mysql format
                    var generatedHash = HashPasswordMySql(providedPassword);
                    if (generatedHash.Equals(hashedPassword)) {
                        // This is an old password hash format - the caller needs to rehash if we're not running in an older compat mode.
                        return PasswordVerificationResult.SuccessRehashNeeded;
                    } else {
                        return PasswordVerificationResult.Failed;
                    }
                default:
                    return base.VerifyHashedPassword(user, hashedPassword, providedPassword); // default
            }
        }
        
        private string HashPasswordMySql(string password) {
            var keyArray = Encoding.UTF8.GetBytes(password);
            var enc = new SHA1Managed();
            var encodedKey = enc.ComputeHash(enc.ComputeHash(keyArray));
            var builder = new StringBuilder(encodedKey.Length);
            foreach (var b in encodedKey) {
                builder.Append(b.ToString("X2"));
            }
            return "*" + builder.ToString();
        }
    }
}
