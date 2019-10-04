namespace Indice.Identity.Security
{
    public class IdentityServerApi
    {
        public const string Scope = "identity";

        public static class SubScopes
        {
            public const string Clients = "identity:clients";
            public const string Users = "identity:users";
        }
    }
}
