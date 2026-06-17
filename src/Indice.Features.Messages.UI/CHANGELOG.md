# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
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
