- [ ] Specification for ICaseAuthorizationService
- [ ] Handle localization in Workflows
- [ ] Pass culture on actors on Workflow
- [ ] Remove Newtonsoft from Cases - Platform
- [ ] Finalize Actor data:
  - Currently, when starting workflow we have http context data + Owner Reference from the contact that created the draft, and when triggering a blocking activity we have http context data + claim ReferenceIdClaimType from the current user


## Improvements
- [ ] Throwing an exception anywhere AFTER a blocking activity will produce a 200 response to Cases. This has to do with how Elsa handles Faulted state, there are events to listen to.
- [ ] Remove return value of `IAdminCaseService.AssignCase` as it always same as the request.
- [ ] Use dispatch for blocking activities.
- 

## Acknowledgements
1. Multiple blocking activities of the same kind are NOT allowed
2. When the integrator wants to specify the output of an activity and handle it as input in another activity data is serialized in Elsa and they have to work with JToken. Sending the data to Cases they will always be converted to JsonNode.
3.  