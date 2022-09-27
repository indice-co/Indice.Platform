using System.Threading.Tasks;
using Indice.Features.Kyc.GovGr.Models;

namespace Indice.Features.Kyc.GovGr.Interfaces
{
    /// <summary>The kyc integration service.</summary>
    public interface IKycService
    {
        /// <summary>
        /// Get Data from eGov KYC
        /// </summary>
        Task<EGovKycResponsePayload> GetEGovKycData(string clientName, string code);
    }
}
