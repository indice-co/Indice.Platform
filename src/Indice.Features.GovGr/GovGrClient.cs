﻿using Indice.Features.GovGr.Configuration;
using Indice.Features.GovGr.Interfaces;
using Indice.Features.GovGr.Proxies.Gsis;
using Microsoft.Extensions.Options;

namespace Indice.Features.GovGr;

/// <summary>GovGR Http Client </summary>
public class GovGrClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<GovGrOptions> _settings;
    private readonly Func<RgWsPublic> _createBusinessRegistrySoapClient;
    private readonly GovGrKycScopeDescriber _kycScopeDescriber;

    /// <summary>Create a GovGrClient</summary>
    /// <param name="httpClientFactory"></param>
    /// <param name="settings"></param>
    /// <param name="createBusinessRegistrySoapClient"></param>
    /// <param name="kycScopeDescriber"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public GovGrClient(IHttpClientFactory httpClientFactory,
        IOptions<GovGrOptions> settings,
        Func<RgWsPublic> createBusinessRegistrySoapClient,
        GovGrKycScopeDescriber kycScopeDescriber) {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _createBusinessRegistrySoapClient = createBusinessRegistrySoapClient ?? throw new ArgumentNullException(nameof(createBusinessRegistrySoapClient));
        _kycScopeDescriber = kycScopeDescriber ?? throw new ArgumentNullException(nameof(kycScopeDescriber));
    }

    internal GovGrOptions Settings => _settings.Value;

    /// <summary>Access the KYC API</summary>
    /// <param name="clientId"></param>
    /// <param name="clientSecret"></param>
    /// <param name="redirectUri"></param>
    /// <param name="environment">Represents the environment. Valid options are <em>production</em>, <em>staging</em>, <em>development</em> &amp; <em>mock</em>. Defaults to <b>production</b>.</param>
    /// <returns>A configured instance of the <see cref="IKycService"/></returns>
    public IKycService Kyc(string clientId, string clientSecret, string redirectUri, string environment = null) => new GovGrKycClient(_httpClientFactory.CreateClient(nameof(GovGrClient)), _kycScopeDescriber, new() {
        Environment = environment ?? _settings.Value.Kyc.Environment,
        ClientId = clientId ?? _settings.Value.Kyc.ClientId,
        ClientSecret = clientSecret ?? _settings.Value.Kyc.ClientSecret,
        RedirectUri = redirectUri ?? _settings.Value.Kyc.RedirectUri
    });

    /// <summary>Access the Wallet API</summary>
    /// <returns>A configured instance of the <see cref="IWalletService"/></returns>
    public IWalletService Wallet(string token, bool? sandbox = null) => new GovGrWalletClient(_httpClientFactory.CreateClient(nameof(GovGrWalletClient)), new() {
        Sandbox = sandbox ?? _settings.Value.Wallet.Sandbox,
        Token = token ?? _settings.Value.Wallet.Token
    });

    /// <summary>Access the Documents API</summary>
    /// <returns>A configured instance of the <see cref="IDocumentsService"/></returns>
    public IDocumentsService Documents(string token, string serviceName) => new GovGrDocumentsClient(_httpClientFactory.CreateClient(nameof(GovGrDocumentsClient)), new() {
        Token = token ?? _settings.Value.Documents.Token,
        ServiceName = serviceName ?? _settings.Value.Documents.ServiceName,
    });

    /// <summary>Access the Bancapp API</summary>
    /// <returns>A configured instance of the <see cref="IBancappService"/></returns>
    public IBancappService Bancapp(string username, string password, string clientId, string environment = null) => new GovGrBancappClient(
        _httpClientFactory.CreateClient(nameof(GovGrBancappClient)), new() {
            Environment = environment ?? _settings.Value.Bancapp.Environment,
            Username = username ?? _settings.Value.Bancapp.Username,
            Password = password ?? _settings.Value.Bancapp.Password,
            ClientId = clientId ?? _settings.Value.Bancapp.ClientId,
        });

    /// <summary>
    ///  Access the Business Registry WS
    /// </summary>
    /// <returns>A configured instance of the <see cref="IBusinessRegistryService"/></returns>
    public IBusinessRegistryService BusinessRegistry() => new GovGrBusinessRegistryClient(_settings.Value.BusinessRegistry, _createBusinessRegistrySoapClient());
}

/// <summary>Extensions on the <see cref="GovGrClient"/></summary>
public static class GovGrClientExtensions
{
    /// <summary>Access the KYC API with default settings. IConfiguration driven</summary>
    /// <param name="client"></param>
    /// <returns></returns>
    public static IKycService Kyc(this GovGrClient client) {
        return client.Kyc(clientId: null, clientSecret: null, redirectUri: null, environment: null);
    }

    /// <summary>Access the Wallet API with default settings. IConfiguration driven</summary>
    /// <returns>A configured instance of the <see cref="IWalletService"/></returns>
    public static IWalletService Wallet(this GovGrClient client) {
        return client.Wallet(token: null, sandbox: null);
    }

    /// <summary>Access the Wallet API. Token is IConfiguration driven</summary>
    /// <returns>A configured instance of the <see cref="IWalletService"/></returns>
    public static IDocumentsService Documents(this GovGrClient client, string serviceName) {
        return client.Documents(token: null, serviceName);
    }
    
    /// <summary>Access the Bancapp API. Token is IConfiguration driven</summary>
    /// <returns>A configured instance of the <see cref="IBancappService"/></returns>
    public static IBancappService Bancapp(this GovGrClient client) {
        return client.Bancapp(username: null, password: null, clientId: null, environment: null);
    }

    /// <summary>
    /// Access the Business Registry WS with default settings. IConfiguration driven
    /// </summary>
    /// <param name="client"></param>
    /// <returns>A configured instance of the <see cref="IBusinessRegistryService"/></returns>
    /// <remarks><a href="https://www.aade.gr/epiheiriseis/forologikes-ypiresies/mitroo/anazitisi-basikon-stoiheion-mitrooy-epiheiriseon">Αναζήτηση Βασικών Στοιχείων Μητρώου Επιχειρήσεων</a></remarks>
    public static IBusinessRegistryService BusinessRegistry(this GovGrClient client) {
        return client.BusinessRegistry();
    }
}
