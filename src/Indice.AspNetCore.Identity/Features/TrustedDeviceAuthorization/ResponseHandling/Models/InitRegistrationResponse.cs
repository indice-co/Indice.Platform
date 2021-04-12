namespace Indice.AspNetCore.Identity.Features
{
    internal class InitRegistrationResponse
    {
        /* https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/init (only for .net 5.0) */
        public string Challenge { get; set; }
    }
}
