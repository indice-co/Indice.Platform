using System.Threading.Tasks;
using Indice.Features.GovGr.Models;

namespace Indice.Features.GovGr.Interfaces
{
    /// <summary>The kyc integration service.</summary>
    public interface IKycService
    {
        /// <summary>
        /// Get Data from eGov KYC
        /// </summary>
        Task<KycPayload> GetData(string clientName, string code);
    }
}
