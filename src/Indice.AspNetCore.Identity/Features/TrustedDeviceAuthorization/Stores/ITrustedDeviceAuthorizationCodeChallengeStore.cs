using System.Threading.Tasks;

namespace Indice.AspNetCore.Identity.Features
{
    public interface ITrustedDeviceAuthorizationCodeChallengeStore
    {
        Task<string> Store(TrustedDeviceAuthorizationCode code);
    }
}
