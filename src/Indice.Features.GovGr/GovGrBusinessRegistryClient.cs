using Indice.Features.GovGr.Configuration;
using Indice.Features.GovGr.Interfaces;
using Indice.Features.GovGr.Models;
using Indice.Features.GovGr.Proxies.Gsis;

namespace Indice.Features.GovGr;

/// <inheritdoc />
internal class GovGrBusinessRegistryClient : IBusinessRegistryService
{
    private readonly GovGrOptions.BusinessRegistryOptions _settings;
    private readonly RgWsPublic _client;
    internal GovGrBusinessRegistryClient(GovGrOptions.BusinessRegistryOptions settings, RgWsPublic client) {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <inheritdoc />
    public async Task<BusinessRegistryRecord> GetBusinessRegistry(string fiscalCode) {
        var input = new RgWsPublicInputRtUser {
            afmCalledBy = _settings.CallersFiscalCode,
            afmCalledFor = fiscalCode
        };
        var request = new rgWsPublicAfmMethodRequest { RgWsPublicInputRt_in = input };
        var result = await _client.rgWsPublicAfmMethodAsync(request);
        if (result.pErrorRec_out != null && !string.IsNullOrWhiteSpace(result.pErrorRec_out.errorCode)) {
            throw new Exception(result.pErrorRec_out.errorDescr);
        }
        return ToModel(result.RgWsPublicBasicRt_out, result.arrayOfRgWsPublicFirmActRt_out);
    }

    private static BusinessRegistryRecord ToModel(RgWsPublicBasicRtUser user, RgWsPublicFirmActRtUserArray firms) {
        var mainFirmActivity = firms?.Where(x => x.firmActKind == "1").FirstOrDefault();
        return new BusinessRegistryRecord {
            TaxOfficeCode = user.doy,
            TaxOfficeDescription = user.doyDescr,
            LegalName = user.onomasia,
            CommercialTitle = user.commerTitle,
            LegalStatusDescription = user.legalStatusDescr,
            DeactivationFlag = user.deactivationFlag,
            DeactivationFlagDescription = user.deactivationFlagDescr,
            Address = new BusinessRegistryAddress {
                Street = user.postalAddress,
                Number = user.postalAddressNo,
                PostalCode = user.postalZipCode,
                City = user.postalAreaDescription
            },
            MainActivityCode = mainFirmActivity?.firmActCode,
            MainActivityDescription = mainFirmActivity?.firmActKindDescr,
            MainActivityKind = mainFirmActivity?.firmActKind,
            RegisterDate = user.registDate,
            StopDate = user.stopDate,
            FirmFlagDescription = user.firmFlagDescr,
            PersonStatusDescription = user.INiFlagDescr
        };
    }
}
