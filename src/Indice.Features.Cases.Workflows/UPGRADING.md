- context.TryGetUser() on ActivityContext is not available anymore
- Bookmark hashes will need recalculation.
- AwaitAssignmentActivity will no longer log exceptions in Elsa Dashboard


Api Calls from Workflows:
- Απο το designer για να προχωρήσουμε το checkpoint
- Από τα activities για να ανεβάσουμε attachment
- Από τα activities για να αλλάξουμε τα CaseData

Aπό custom workflows:
- getCaseById
- getAttachments, GetAttachment
- PatchCaseMetadata
- PatchCaseData

NotificationSubscriptionService.GetSubscriptions() is also used.