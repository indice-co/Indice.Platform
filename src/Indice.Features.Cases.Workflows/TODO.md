- [ ] Register Workflow client_credentials
- [ ] Specification for ICaseAuthorizationService
- [ ] Handle localization in Workflows
- [ ] Pass culture on actors on Workflow
- [ ] Add authorization for all user action endpoints
- [ ] Finalize Actor data:
  - Currently, when starting workflow we have http context data + Owner Reference from the contact that created the draft, and when triggering a blocking activity we have http context data + claim ReferenceIdClaimType from the current user

## Acknowledgements
Multiple blocking activities of the same kind are NOT allowed