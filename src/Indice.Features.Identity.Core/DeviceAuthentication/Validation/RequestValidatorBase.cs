﻿using System.Collections.Specialized;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServer4.Validation;

namespace Indice.Features.Identity.Core.DeviceAuthentication.Validation;

internal abstract class RequestValidatorBase<TValidationResult> : RequestChallengeValidator where TValidationResult : ValidationResult, new()
{
    protected RequestValidatorBase(
        IClientStore clientStore,
        ITokenValidator tokenValidator
    ) {
        ClientStore = clientStore ?? throw new ArgumentNullException(nameof(clientStore));
        TokenValidator = tokenValidator ?? throw new ArgumentNullException(nameof(clientStore));
    }

    protected IClientStore ClientStore { get; }
    protected ITokenValidator TokenValidator { get; }

    public abstract Task<TValidationResult> Validate(NameValueCollection parameters, string? accessToken = null);

    protected async Task<Client?> LoadClient(string? clientId) {
        if (clientId == null) {
            return null;
        }
        var client = await ClientStore.FindEnabledClientByIdAsync(clientId);
        return client;
    }

    protected Task<Client?> LoadClient(TokenValidationResult tokenValidationResult) {
        var clientId = tokenValidationResult.Claims.Single(x => x.Type == JwtClaimTypes.ClientId).Value;
        return LoadClient(clientId);
    }

    protected TValidationResult Error(string error, string? errorDescription = null) => new() {
        IsError = true,
        Error = error,
        ErrorDescription = errorDescription
    };
}
