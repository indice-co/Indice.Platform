# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
## [Unreleased]

### Breaking changes

#### Inbox attachments are downloaded with the user's access token
The attachment endpoints of the messaging api no longer allow anonymous access (see the `Indice.Features.Messages.AspNetCore`
changelog). The "Attachment" button of the inbox reading pane no longer opens the attachment url in a new tab; it downloads the
file through the authenticated http client and saves it. Every attachment, whatever its type, is saved as a file download and is
never opened in the browser. Attachment urls that do not belong to the messaging api are not downloaded, so the access token is
never sent to other hosts. Update the UI together with the api, since older UI versions receive `401` when opening attachments.

### Fixes

#### Campaign details attachment download
The download button in the "Attachments" section of the campaign details page did nothing. The attachment permalink already starts
with the api path prefix (e.g. `/api/campaigns/attachments/...`), but it was appended to the api origin with an extra `/`. That gave
`https://host//api/...` (404) when `api_url` was absolute, and a protocol-relative `//api/...` url with no domain when `api_url` was
relative. The permalink is now resolved against `api_url` (or the app origin), downloaded only if it belongs to the messaging api,
and saved as a file download like the inbox attachments. Failures now show an error toast.

## [8.64.0]

### Personal inbox
The admin UI now has a notifications bell (unread count and, when opened, a preview of the latest unread messages) and a "My inbox"
page (filter by status, type, text and expired messages; read / unread / delete) built on the `my/messages` api. The page is reachable
from the bell and from the user menu (next to sign out). The unread count is fetched with a page size of zero (count only).
`CampaignUIOptions.EnableInbox` (default `true`) toggles it. Set `CampaignUIOptions.InboxHubUrl` (e.g. `/hubs/messaging`, relative
urls resolve against the api origin) to receive real time updates, otherwise the UI refreshes the unread count
every 5 minutes and when the tab becomes visible. These background requests do not show the top progress bar.

```csharp
app.UseMessagesUI(options => { options.InboxHubUrl = "/hubs/messaging"; });
```

## [8.50.0] - 2026-06-16

### Disabling the MapTranslationsGraph endpoints.
By default, the system will automatically map the built-in translation endpoints.<br>
However, if you have integrated your own custom translation service and wish to bypass the provided endpoints entirely, 
you can easily opt out of this default behavior. Simply update your startup configuration by setting `MessageEndpointOptions.MapTranslations` to false

```csharp
builder.Services.AddMessaging(options => { options.MapTranslations = false;
});
```

## [7.3.7] - 2023-08-02
### Added
- New settings section added to configure desired email sendes.
- New functionality to select the desired sender name for email campaign.
- Upload attachment functionality was added on create new campaign process.
- Resolve contacts UI was added on create new campaign process.

### Fixed
- Fixed bug on edit campaign page - campaign metadata where not shown.
