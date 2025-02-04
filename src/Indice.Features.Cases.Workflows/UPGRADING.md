- context.TryGetUser() on ActivityContext is not available anymore
- Bookmark hashes will need recalculation.
- AwaitAssignmentActivity will no longer log exceptions in Elsa Dashboard


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