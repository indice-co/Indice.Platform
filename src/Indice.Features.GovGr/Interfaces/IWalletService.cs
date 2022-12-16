using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Indice.Features.GovGr.Models;
using Microsoft.Extensions.Localization;
using Polly.Utilities;

namespace Indice.Features.GovGr.Interfaces
{
    /// <summary>
    /// GovGr wallet service definition
    /// </summary>
    public interface IWalletService
    {
        /// <summary>
        /// Creates a request for identification document retrieval 
        /// </summary>
        /// <param name="idNumber">The hellenic identification number</param>
        /// <returns>A reference id pointing to the document retreival</returns>
        /// <exception cref="Types.GovGrServiceException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        Task<WalletDocumentReference> RequestIdentificationAsync(string idNumber);
        /// <summary>
        /// Retreives the identification document
        /// </summary>
        /// <param name="declarationId">The declaration/reference id to the document request</param>
        /// <param name="confirmationCode">OTP sent to the user</param>
        /// <param name="exportDocumentPdf">Request for the binary pdf document to accompany the json data.</param>
        /// <returns>Payload containing all doucment related data. As well as the optional attachment/pdf</returns>
        /// <exception cref="Types.GovGrServiceException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        Task<DocumentData> GetIdentificationAsync(string declarationId, string confirmationCode, bool? exportDocumentPdf = null);
    }
}
