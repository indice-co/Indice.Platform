﻿using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Cases.Server.Endpoints;
internal static class LookupHandler
{
    public static async Task<Ok<ResultSet<LookupItem>>> GetLookup(
        string lookupName,
        [AsParameters] ListOptions options,
        [AsParameters] LookupFilter filter,
        ILookupServiceFactory lookupServiceFactory) {
        var lookupService = lookupServiceFactory.Create(lookupName);
        var lookupItems = await lookupService.Get(ListOptions.Create(options, filter));
        return TypedResults.Ok(lookupItems);
    }
}
