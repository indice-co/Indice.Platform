using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Indice.Services;

namespace Indice.AspNetCore.Identity
{
    public class MockTotpService : ITotpService
    {
        public Task<Dictionary<string, TotpProviderMetadata>> GetProviders(ClaimsPrincipal principal) => 
            Task.FromResult(new Dictionary<string, TotpProviderMetadata>());

        public Task<TotpResult> Send(ClaimsPrincipal principal, string message, TotpDeliveryChannel channel = TotpDeliveryChannel.Sms, string purpose = null, string securityToken = null, string phoneNumberOrEmail = null, string data = null, string classification = null) =>
            Task.FromResult(TotpResult.SuccessResult);

        public Task<TotpResult> Verify(ClaimsPrincipal principal, string code, TotpProviderType? provider = null, string purpose = null, string securityToken = null, string phoneNumberOrEmail = null) => 
            Task.FromResult(TotpResult.SuccessResult);
    }
}
