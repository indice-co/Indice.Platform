# Indice.Features.GovGr

Gov.gr services integration .Net Library.

Currently supports:
- eGov-KYC
- Gov.gr Wallet
- Business Registry

## Prerequisites

### Installation

To install Indice.Features.GovGr, run the following command in the Package Manager Console:

```powershell
PM> Install-Package "Indice.Features.GovGr"
```

Or, simply, download it [here](https://www.nuget.org/packages/Indice.Features.GovGr/):

### eGov-KYC

For the eGov-KYC integration (authorization code flow) to work, you will need to follow the steps bellow:

Firstly, you need to have one (or more) OAuth2 Client(s) registered by GSIS. You can use the appsettings in the following format to store client information:

```
  "KycSettings": {
    "Environment": "demo", // production, staging, demo, mock
    "Clients": [
      {
        "Name": "ClientA",
        "ClientId": "ClientIdA",
        "ClientSecret": "ClientSecretA",
        "RedirectUri": "RedirectUriA"
      },
      {
        "Name": "ClientB",
        "ClientId": "ClientIdB",
        "ClientSecret": "ClientSecretB",
        "RedirectUri": "RedirectUriB"
      }
    ]
  }
```

Note: If `Environment` is set to `production` or `staging` you have to use production credentials!

Secondly, you have to make the call to /authorize endpoint (typically from a SPA or a mobile app).

JavaScript Example:

```javascript
    const selectedScopes = 'identity income contactInfo professionalActivity';
    const eGovKycUrl = new URL(environment.e_gov_kyc_settings.authorize_endpoint);

    eGovKycUrl.searchParams.append('response_type', environment.e_gov_kyc_settings.response_type); // typically 'code'
    eGovKycUrl.searchParams.append('redirect_uri', environment.e_gov_kyc_settings.redirect_uri);
    eGovKycUrl.searchParams.append('client_id', environment.e_gov_kyc_settings.client_id);
    eGovKycUrl.searchParams.append('scope', selectedScopes);

    window.location.href = eGovKycUrl.href;
```

Once you retrieve the authorization code and return to your application, you pass it to your back-end, in order to make the back-channel call (/token endpoint). To achieve that, simply, inject `GovGrClient _govGrClient` to your back-end code and use it to get user data:

```csharp
var kycPayload = await _govGrClient.Kyc(clientId, clientSecret, redirectUri, environment).GetDataAsync(code);
```

You can also get the available scopes from ` _govGrClient` (used in the call to /authorize endpoint):

```csharp
var kycScopes = await _govGrClient.Kyc().GetAvailableScopes();
```

### Gov.gr Wallet

For the Gov.gr Wallet integration to work, you will need to follow the steps bellow:

You have to use the appsettings in the following format:

```
  "GovGr": {
    "Wallet": {
      "Sandbox": true,
      "Token": "<The service token goes here>"
    }
  }
```

Note: If `Sandbox` is set to `false` you integrate with production services!

Then, simply, inject `GovGrClient _govGrClient` to your code and use it to get user document data:

```csharp
WalletDocumentReference reference = await _govGrClient.Wallet().RequestIdentificationAsync(idNumber);
```

Once, you get the reference/declaration Id and receive the OTP:

```csharp
DocumentData data = await _govGrClient.Wallet().GetIdentificationAsync(declarationId, otp, includePdf);
```