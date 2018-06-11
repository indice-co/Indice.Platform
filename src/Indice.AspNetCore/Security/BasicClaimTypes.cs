using System;
using System.Collections.Generic;
using System.Text;

namespace Indice.AspNetCore.Security
{
    public static class BasicClaimTypes
    {
        public const string Prefix = "indice_";

        public const string Admin = "admin";
        public const string System = "system";
        
        public static readonly string[] UserClaims = {
            "sub",
            "name",
            "email",
            "phone",
            "phone_verified",
            "email_verified",
            "family_name",
            "given_name",
            "role",
            Admin
        };
    }
}
