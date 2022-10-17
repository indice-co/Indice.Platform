using System.Text.Json.Serialization;
using Indice.Features.Kyc.GovGr.Extensions;

namespace Indice.Features.Kyc.GovGr.Enums
{
    [JsonConverter(typeof(StringKycStatusCodeConverter))]
    public enum KycStatusCode
    {
        Unknown,
        SuccessAndData = 1200,
        SuccessButNoData = 1201,
        SuccessButPartialData = 1202,
        CommunicationFailureWithGSIS = 1400,
        ConnectionFailureWithGSIS = 1401,
        ErroneousResponseFromGSIS = 1402,
        FailureInGSISInteroperabilityCenter = 1410,
        CommunicationFailureBetweenGSISAndInformationProvider = 1411,
        CommunicationFailureBetweenGSISAndInfrastructure = 1412,
        ErrorInGSIS = 1413,
        ErrorInGSISInteroperabilityCenter = 1510,
        NoAuthenticationToGSIS = 1511,
        NoAuthorizationToGSIS = 1512,
        ErroneousRequestToGSIS = 1513,
        InvalidInputToGSIS = 1514,
        CallLimitExceedanceToGSIS = 1515,
        ErrorInInformationProviderInfrastructure = 1520,
        ErrorInInformationProviderData = 1521,
        NoKycPermission = 1530,
        NoEnoughData = 1539,
    }
}

