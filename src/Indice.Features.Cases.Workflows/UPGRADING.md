- context.TryGetUser() on ActivityContext is not available anymore, use context.TryGetLastActor() optionally resolving with your identity provider
- Bookmark hashes will need recalculation.
- Remove `When` from activities that had `AuditMeta` as an input, this will be automatically set to the current DateTimeOffset i.e. AssignCaseToUserActivity, 
- Case rejection reasons resources should be copied to `Workflow` as well using `WorkflowSharedResource`

## Integrators endpoints currently used
Api Calls from Workflows SendMessageActivity:
- Απο το designer για να προχωρήσουμε το checkpoint
- Από τα activities για να ανεβάσουμε attachment - που?
- Από τα activities για να αλλάξουμε τα CaseData

Aπό custom workflows:
- getCaseById
- getAttachments, GetAttachment
- PatchCaseMetadata
- PatchCaseData

NotificationSubscriptionService.GetSubscriptions() is also used.

## Integrators Current needed actions
System User, As a workflow integrator I want:
- to know the last actor on a case the last ClaimsPrincipal that performed an action.
- to know the last approver of a case.
- to be able to add an approval.
- to be able to remove an assignment for a case.
- to be able to rollback an approval action.
- to be able to add a comment to a case.
- to be able to move a checkpoint of a case.
- to be able to upload an attachment?
- to be able to update the case data.
- to be able to change the time of retries for an http call to cases.