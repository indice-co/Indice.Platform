## Improvements
- [ ] Throwing an exception anywhere AFTER a blocking activity will produce a 200 response to Cases. This has to do with how Elsa handles Faulted state, there are events to listen to.
- [ ] Remove return value of `IAdminCaseService.AssignCase` as it always same as the request.
- [ ] Use dispatch for blocking activities.
- [ ] Authorization Requirements are checked when displaying Available Actions to the user but not all of them on the specific endpoints i.e. Edit, Assign, Approve
- [ ] Remove Newtonsoft from Cases - Platform, this shouldn't be used anymore
- [ ] Add PatchMyData method to the CasesManager endpoint.
- [ ] NotificationSubscriptionService.GetSubscriptions() could also be a CasesManager endpoint.

## Acknowledgements
1. Multiple blocking activities of the same kind are NOT allowed
2. When the integrator wants to specify the output of an activity and handle it as input in another activity data is serialized in Elsa and they have to work with JToken. Sending the data to Cases they will always be converted to JsonNode.
