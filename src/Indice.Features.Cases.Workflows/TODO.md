- [ ] Specification for ICaseAuthorizationService
- [ ] Handle localization in Workflows
- [ ] Pass culture on actors on Workflow
- [ ] Add authorization for all user action endpoints
- [ ] Remove Newtonsoft from Cases
- [ ] Finalize Actor data:
  - Currently, when starting workflow we have http context data + Owner Reference from the contact that created the draft, and when triggering a blocking activity we have http context data + claim ReferenceIdClaimType from the current user
- [ ] Properly handle Business Exceptions in the cases client

## Improvements
- [ ] Throwing an exception anywhere AFTER a blocking activity will produce a 200 response to Cases.
- [ ] Remove return value of `IAdminCaseService.AssignCase`

## Acknowledgements
1. Multiple blocking activities of the same kind are NOT allowed
2. When the integrator wants to specify the output of an activity and handle it as input in another activity data is serialized in Elsa and they have to work with JToken. Sending the data to Cases they will always be converted to JsonNode.

## Flows
1. Edit Triggered from Spa:
Receive JsonNode in Edit Case and Execute Edit Activity Invoker
Elsa serializes and saves data using NewtonSoft custom Converters
Deserializes using custom NewtonSoft converters from the attribute on the model to JsonNode gives the data in activity context
Data is passed to Cases through http client, cases dynamic data is retrieved as JsonElement

2. In Workflow Designer: GetCaseDetails --> SendMessage to change case data --> GetCaseDetails
Case data is retrieved from Cases as JsonElement - dynamic
Data is passed to Workflow through http client and result as JsonElement
Data is converted to JToken in GetCaseDetailsActivity so it can work with Jint expressions
SendMessageActivity gets the Activity Output of GetCaseDetailsActivity as JToken. This is for product activity, developer can normally call sendMessageAsync() with whatever data
Data is sent to cases through http client being converted on the client if needed.

When the integrator retrieves data from Cases it should be JsonNode everywhere.
When the integrator wants to specify the output of an activity and handle it as input in another activity data is serialized in Elsa and he has to work with JToken. Sending the data to Cases they will always be converted to JsonNode.