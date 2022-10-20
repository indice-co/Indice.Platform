using System.Text.Json.Serialization;
using Indice.Features.GovGr.Serialization;

namespace Indice.Features.GovGr.Models
{
    /// <summary>
    /// GovGr KYC StatusCodes
    /// </summary>
    [JsonConverter(typeof(StringKycStatusCodeConverter))]
    public enum KycStatusCode
    {
        /// <summary><see cref="Unknown"/></summary>
        Unknown,
        /// <summary><see cref="SuccessAndData"/></summary>
        SuccessAndData = 1200,
        /// <summary><see cref="SuccessButNoData"/></summary>
        SuccessButNoData = 1201,
        /// <summary><see cref="SuccessButPartialData"/></summary>
        SuccessButPartialData = 1202,
        /// <summary><see cref="CommunicationFailureWithGSIS"/></summary>
        CommunicationFailureWithGSIS = 1400,
        /// <summary><see cref="ConnectionFailureWithGSIS"/></summary>
        ConnectionFailureWithGSIS = 1401,
        /// <summary><see cref="ErroneousResponseFromGSIS"/></summary>
        ErroneousResponseFromGSIS = 1402,
        /// <summary><see cref="FailureInGSISInteroperabilityCenter"/></summary>
        FailureInGSISInteroperabilityCenter = 1410,
        /// <summary><see cref="CommunicationFailureBetweenGSISAndInformationProvider"/></summary>
        CommunicationFailureBetweenGSISAndInformationProvider = 1411,
        /// <summary><see cref="CommunicationFailureBetweenGSISAndInfrastructure"/></summary>
        CommunicationFailureBetweenGSISAndInfrastructure = 1412,
        /// <summary><see cref="ErrorInGSIS"/></summary>
        ErrorInGSIS = 1413,
        /// <summary><see cref="ErrorInGSISInteroperabilityCenter"/></summary>
        ErrorInGSISInteroperabilityCenter = 1510,
        /// <summary><see cref="NoAuthenticationToGSIS"/></summary>
        NoAuthenticationToGSIS = 1511,
        /// <summary><see cref="NoAuthorizationToGSIS"/></summary>
        NoAuthorizationToGSIS = 1512,
        /// <summary><see cref="ErroneousRequestToGSIS"/></summary>
        ErroneousRequestToGSIS = 1513,
        /// <summary><see cref="InvalidInputToGSIS"/></summary>
        InvalidInputToGSIS = 1514,
        /// <summary><see cref="CallLimitExceedanceToGSIS"/></summary>
        CallLimitExceedanceToGSIS = 1515,
        /// <summary><see cref="ErrorInInformationProviderInfrastructure"/></summary>
        ErrorInInformationProviderInfrastructure = 1520,
        /// <summary><see cref="ErrorInInformationProviderData"/></summary>
        ErrorInInformationProviderData = 1521,
        /// <summary><see cref="NoKycPermission"/></summary>
        NoKycPermission = 1530,
        /// <summary><see cref="NoEnoughData"/></summary>
        NoEnoughData = 1539,
    }
}

