using System.Threading.Tasks;
using Indice.Integration.EGov.Kyc.Models;

namespace Indice.Integration.EGov.Kyc.Interfaces
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
