using System.Collections.Generic;

namespace Indice.AspNetCore.Identity.Models
{
    public class RegisterRequest
    {
        public class Attribute
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string ReturnUrl { get; set; }

        /// <summary>
        /// List of claims where each item is formatted as claimType:claimValue collection of strings.
        /// </summary>
        public List<Attribute> Claims { get; set; } = new List<Attribute>();

        protected void ReplaceClaim(string name, string value) {
            Claims.RemoveAll(c => c.Name.Equals(name));
            Claims.Add(new Attribute { Name = name, Value = value });
        }
    }

    public class ChangePasswordModel
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }

    public class ForgotPasswordRequest
    {
        public string Email { get; set; }
    }

    public class ResetPasswordModel
    {
        public string Username { get; set; }
        public string Token { get; set; }
        public string NewPassword { get; set; }
    }

    public class VerifyTokenModel
    {
        public string Code { get; set; }
    }

    public class ForgotPasswordVerifyModel : VerifyTokenModel
    {
        public string Email { get; set; }
        public string NewPassword { get; set; }
    }
}
