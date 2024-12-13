using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Xml;
using Indice.Features.GovGr.Configuration;

namespace Indice.Features.GovGr.Proxies.Gsis;

/// <summary></summary>
public partial class RgWsPublicClient
{
    /// <summary></summary>
    public RgWsPublicClient(GovGrOptions.BusinessRegistryOptions settings) : base(GetBindingForEndpoint(new EndpointAddress(settings.BaseAddress)), new EndpointAddress(settings.BaseAddress)) {
        Endpoint.Name = nameof(RgWsPublic);
        var requestInterceptor = new InspectorBehavior(new MessageInspector(settings.Username!, settings.Password!));
        Endpoint.EndpointBehaviors.Add(requestInterceptor);
        ConfigureEndpoint(Endpoint, ClientCredentials);
    }

    private static Binding GetBindingForEndpoint(EndpointAddress endpointAddress) {
        HttpBindingBase result;
        if (endpointAddress.Uri.Scheme == "https") {
            result = new BasicHttpsBinding();
        } else {
            result = new BasicHttpBinding {
                Security = new BasicHttpSecurity {
                    Mode = BasicHttpSecurityMode.Transport
                }
            };
        }
        result.MaxBufferSize = int.MaxValue;
        result.ReaderQuotas = XmlDictionaryReaderQuotas.Max;
        result.MaxReceivedMessageSize = int.MaxValue;
        result.AllowCookies = true;
        return result;
    }
}