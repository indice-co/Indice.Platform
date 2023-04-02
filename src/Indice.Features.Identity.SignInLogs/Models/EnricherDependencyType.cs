namespace Indice.Features.Identity.SignInLogs.Models;

[Flags]
internal enum EnricherDependencyType
{
    None = 0,
    Default = 1 << 0,
    OnRequest = 1 << 1,
    OnDataStore = 1 << 2
}
