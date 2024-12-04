﻿using Indice.Features.Cases.Core.Models.Requests;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Cases.Controllers;

/// <summary>Manage Notifications for Back-office users.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = ApiGroups.CasesApiGroupNamePlaceholder)]
[Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeCasesManager)]
[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
[Route($"{ApiPrefixes.CasesApiTemplatePrefixPlaceholder}/manage/my/notifications")]
internal class AdminNotificationsController : ControllerBase
{
    private readonly INotificationSubscriptionService _service;
    private readonly AdminCasesApiOptions _casesApiOptions;

    public AdminNotificationsController(
        INotificationSubscriptionService service,
        AdminCasesApiOptions casesApiOptions) {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _casesApiOptions = casesApiOptions ?? throw new ArgumentNullException(nameof(casesApiOptions));
    }

    /// <summary>Get the notification subscriptions for a user.</summary>
    /// <returns></returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(NotificationSubscriptionResponse))]
    public async Task<IActionResult> GetMySubscriptions() {
        var options = new ListOptions<NotificationFilter> {
            Filter = NotificationFilter.FromUser(User, _casesApiOptions.GroupIdClaimType)
        };
        var result = await _service.GetSubscriptions(options);
        return Ok(new NotificationSubscriptionResponse {
            NotificationSubscriptions = result
        });
    }

    /// <summary>Store user's subscription settings.</summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Subscribe(NotificationSubscriptionRequest request) {
        if ((request.CaseTypeIds?.Count > 0)) {
            ModelState.AddModelError(nameof(request.CaseTypeIds), "At least one CaseTypeId is required in order to subscribe");
            return BadRequest(ModelState);
        }
        await _service.Subscribe(request.CaseTypeIds!, NotificationSubscription.FromUser(User, _casesApiOptions.GroupIdClaimType));
        return NoContent();
    }
}