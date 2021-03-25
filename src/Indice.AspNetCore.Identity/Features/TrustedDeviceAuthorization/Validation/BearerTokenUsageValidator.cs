// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using IdentityModel;
using Indice.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace IdentityServer4.Validation
{
    /// <summary>
    /// Validates a request that uses a bearer token for authentication.
    /// </summary>
    internal class BearerTokenUsageValidator
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BearerTokenUsageValidator"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public BearerTokenUsageValidator(ILogger<BearerTokenUsageValidator> logger) {
            _logger = logger;
        }

        /// <summary>
        /// Validates the request.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        public async Task<BearerTokenUsageValidationResult> Validate(HttpContext context) {
            var result = ValidateAuthorizationHeader(context);
            if (result.TokenFound) {
                _logger.LogDebug("Bearer token found in header.");
                return result;
            }
            if (context.Request.HasApplicationFormContentType()) {
                result = await ValidatePostBody(context);
                if (result.TokenFound) {
                    _logger.LogDebug("Bearer token found in body.");
                    return result;
                }
            }
            _logger.LogDebug("Bearer token was not found.");
            return new BearerTokenUsageValidationResult();
        }

        /// <summary>
        /// Validates the authorization header.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        public BearerTokenUsageValidationResult ValidateAuthorizationHeader(HttpContext context) {
            var authorizationHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(authorizationHeader)) {
                var header = authorizationHeader.Trim();
                if (header.StartsWith(OidcConstants.AuthenticationSchemes.AuthorizationHeaderBearer)) {
                    var value = header[OidcConstants.AuthenticationSchemes.AuthorizationHeaderBearer.Length..].Trim();
                    if (!string.IsNullOrWhiteSpace(value)) {
                        return new BearerTokenUsageValidationResult {
                            TokenFound = true,
                            Token = value,
                            UsageType = BearerTokenUsageType.AuthorizationHeader
                        };
                    }
                } else {
                    _logger.LogTrace("Unexpected header format: {header}", header);
                }
            }
            return new BearerTokenUsageValidationResult();
        }

        /// <summary>
        /// Validates the post body.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        public static async Task<BearerTokenUsageValidationResult> ValidatePostBody(HttpContext context) {
            var token = (await context.Request.ReadFormAsync())["access_token"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(token)) {
                return new BearerTokenUsageValidationResult {
                    TokenFound = true,
                    Token = token,
                    UsageType = BearerTokenUsageType.PostBody
                };
            }
            return new BearerTokenUsageValidationResult();
        }
    }
}
