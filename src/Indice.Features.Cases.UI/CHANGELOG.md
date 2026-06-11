# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [8.49.1] - 2026-06-11
### What changed

The Cases UI can now be served in **English and Greek**, switchable at runtime. UI strings live in
embedded `.resx` files on the server and are delivered to the SPA as a JSON graph; the SPA consumes
them through `@ngx-translate`.

| Area | Change |
|---|---|
| `AddCaseServer(...)` | Now also calls `AddTranslationGraph(...)` internally to register the Cases translation graph. |
| `MapCases(...)` | Now also calls `MapTranslationGraph()`, exposing two new endpoints (below). |
| New embedded resources | `Resources/Cases.Ui.TranslationApi.resx` (neutral / English) and `Resources/Cases.Ui.TranslationApi.el.resx` (Greek) in `Indice.Features.Cases.Server`. |
| Cases SPA | Added `@ngx-translate/core` v17 + `@ngx-translate/http-loader`, an `AppLanguagesService` and runtime language switching. |

#### New endpoints

Both are mapped automatically by `MapCases()` and follow your configured `PathPrefix` (default `/api`):

| Method & route | Purpose |
|---|---|
| `GET {prefix}/cases-i18n.{lang}.json` | Returns the UI translation strings for `{lang}` (e.g. `en`, `el`) as a nested JSON object graph. Output-cached for 24h. |
| `GET {prefix}/languages` | Returns the list of available UI languages (`lang`, `nativeName`, `englishName`), derived from the host's configured supported cultures. |

> Both endpoints are excluded from the OpenAPI/Swagger description by default.

### Backend migration

If you wire Cases the standard way, **there is nothing to do** — the endpoints are registered and
mapped for you:

```csharp
builder.AddCaseServer(options => options.PathPrefix = "/api"); // registers the translation graph
// ...
app.MapCases(); // maps /api/cases-i18n.{lang}.json and /api/languages
```

Check the following in your host:

1. **Supported cultures.** The `/languages` endpoint reflects
   `RequestLocalizationOptions.SupportedCultures`. Make sure your host registers the cultures you want
   offered (at minimum `en` and `el`).
2. **Localization / `ResourcesPath`.** The shipped resources resolve under the `Resources` folder.
   `IndiceWebApplicationBuilder` / `AddMinimalApiDefaults()` already configures
   `AddLocalization(o => o.ResourcesPath = "Resources")`, so standard hosts need no action. If you build
   a non-standard host, ensure localization is configured equivalently.
3. **Custom `PathPrefix`.** Both new routes inherit your prefix. If you changed it, your SPA's loader
   URL must match (see below).

### Frontend migration

The Cases SPA is configured out of the box:

1. **Dependencies:** `@ngx-translate/core@^17` and `@ngx-translate/http-loader`.
2. **Translate provider** (in `app.module.ts`) — point the loader at the backend endpoint and set a
   fallback language:

   ```ts
   provideTranslateService({
     loader: provideTranslateHttpLoader({
       prefix: `${app.settings.api_url}/cases-i18n.`, // resolves to /api/cases-i18n.{lang}.json
       useHttpBackend: true
     }),
     fallbackLang: 'en'
   })
   ```
3. **`AppLanguagesService`** — provided for `APP_LANGUAGES`; calls `/languages` (over a bare
   `HttpBackend` so it bypasses the auth / accept-language interceptors), exposes the menu options, and
   drives `translate.use(...)` on selection. It also re-applies the user's `locale` claim on sign-in.

   > For labels built in TypeScript via `translate.instant(...)` (not the `| translate` pipe), subscribe
   > to `AppLanguagesService.onLanguageChange()` and rebuild them so they re-translate live on switch.

### Overriding / extending translations

To override shipped keys or add your own, layer a second resource onto the **same** `cases-i18n`
endpoint with `PostConfigure` (so you augment rather than clobber the Cases config):

```csharp
builder.Services.PostConfigure<TranslationsGraphOptions>(options => {
    // endpointRoutePattern: null -> reuse the default cases-i18n endpoint
    options.AddResource("MyApp.Cases.Translations", endpointRoutePattern: null, translationsLocation: "MyApp.Host");
});
```

How the merge works: `GetEndpoints()` orders resources `[Cases default, ...your additions]`, the handler
concatenates their strings in that order, and `ToObjectGraph()` is **last-wins** on duplicate keys — so
your resource overrides the Cases defaults for matching keys.

**Make the added resource actually resolve, or the whole endpoint 500s.** All resources sharing the
endpoint are resolved in one pass; if any one can't find its embedded manifest, the entire request
throws `MissingManifestResourceException` (taking the working Cases strings down with it). For your
resource to resolve:

- Provide a **neutral** `.resx` (e.g. `MyApp.Cases.Translations.resx`), not only culture-specific ones —
  the neutral file anchors the culture hierarchy.
- Mark the `.resx` files as **`EmbeddedResource`**.
- With `ResourcesPath = "Resources"` (the platform default), place them under a **`Resources`** folder so
  the manifest name is `{Assembly}.Resources.{baseName}`. Pass the base name *without* the `Resources.`
  prefix — the `ResourcesPath` adds it.
- Pass the correct **assembly name** as `translationsLocation`, and ensure that assembly's `RootNamespace`
  matches its assembly name.


## [8.42.2] - 2026-06-04
### Fixed
- A bug when the configuration had `ReferenceNumber` filter enabledm, the filter was not shown.

## [7.31.2] - 2024-10-11
### Added
- `UnauthorizedComponent` from `Indice.Angular` library to handle Forbidden requests.

## [7.28.3] - 2024-08-30
### Added
- `GridColumnConfig` property to CaseType, you can change your lib-list-view to display custom columns.

## [7.23.3] - 2024-06-20
### Added
- The selected specific case type that is selected by the side navbar menu (navlinks) is now being displayed in the title
- When clicking on specific case type from the menu, the filter can now no longer be removed

## [7.23.2] - 2024-06-05
### Added
- `IsMenuItem` property to CaseType, you can now have all your cases displayed in a separate category as a menu item based on their case type
- `GridFilterConfig` property to CaseType, you can add a case type specific filter to your searchOptions dropdown.

> For example, you can edit a case type from the UI and put a `SearchOption` json formatted string like so:
```
[
    {
        "field": "RequestId",
        "name": "REQUEST ID",
        "dataType": "string"
    }
]
```

## [7.20.2] - 2024-02-22
### Changed
- Global renaming on case name greek wording.
  - `Αίτηση` -> `Υπόθεση`
- Row height of Case & Case Type list View has been reduced.
- Case custom Action buttons have been aligned to the left to conform with case form elements.

### Added
- `label-only` widget component has been extended to handle `href` elements.
- `label-only` widget component has been extended to handle elements that need data-binding from other form data, eg. `extraType = "data-bind"`.

### Bugfix
- Number data types on `label-only` widget component are now displayed correctly.

## [7.20.1] - 2024-02-14
### Added
- `label-only` widget component to handle form input data as a simple label in cases BackOffice. Supports _enum_, _currency_ & _bool_ type conversions.
- **Disables** Add button from _readonly array_ types & other improvements.

## [7.4.1] - 2023-08-29
### Added
- `href` widget component to handle links in cases backoffice.

## [7.4.0] - 2023-08-29
### Added
- SPA settings to control which filters & columns are visible in the cases list view.
- ReferenceNumber translation using the key `cases.referenceNumber` & `caseDetails.referenceNumber`
- CasesUIOptions to control which filters & columns are visible in the cases list view.
> Example to override the visible filters & columns
```cs
app.UseCasesUI(options => {
    // Filter using only: ReferenceNumber, CustomerId, CustomerName,
    // TaxId, CaseTypeCode
    options.CaseListFilters = new HashSet<CaseListFilter>() {
        CaseListFilter.ReferenceNumber,
        CaseListFilter.CustomerId,
        CaseListFilter.CustomerName,
        CaseListFilter.TaxId,
        CaseListFilter.CaseTypeCodes,
    };
    // Display only the columns: ReferenceNumber, CustomerId,
    // CustomerName, TaxId, CaseType, AssignedTo. SubmitDate
    options.CaseListColumns = new HashSet<CaseListColumn>() {
        CaseListColumn.ReferenceNumber,
        CaseListColumn.CustomerId,
        CaseListColumn.CustomerName,
        CaseListColumn.TaxId,
        CaseListColumn.CaseType,
        CaseListColumn.AssignedTo,
        CaseListColumn.SubmitDate
    };
});
```

## [7.3.6] - 2023-07-06
### Bugfix
- PDF button visibility is calculated from the correct property `checkpointType.status`.

## [7.3.5] - 2023-07-03
### Bugfix
- Tailwind `ng-indice` classes were overriding back-office action buttons, making them transparent.

## [7.3.4] - 2023-07-03
### Added
- "not-equals" and "contains" operators to GetCases list filters.

## [7.3.2] - 2023-06-29
### Added
- Overridable translation support for `dashboard`, `cases`, `case-details` pages using `@ngx-translate`.
> Example to override default el.json file
```cs
app.UseCasesUI(options => {
    // This is the absolute path to the folder that contains the el.json
    options.I18nAssets = "/assets/cases/i18n";
});
```
- Added the ability to control which canvases (dashboardTags) should be enabled for the dashboard
> Example enabling only two canvases
```cs
app.UseCasesUI(options => {
    options.DashboardTags = new List<string>() {
        "GroupedByCasetype",
        "GroupedByStatus"
    };
```
- `CheckpointType` Translations to cases list, checkpointtype filter, case details and timeline.

## [7.1.8] - 2023-05-26
### Bugfix
- CaseForm now allows empty layout and shows json schema correctly

## [7.0.5] - 2023-03-30
### Added
- `checkpointsThatAllowDownload` as a case type Config option 

## [7.0.4 - 2023-03-27]
### Added
- Notification subscriptions now use case type Categories & Ordering

## [7.0.3 - 2023-03-27]
### Added
- layoutNode option `case-channel` that indicates whether the field is visible based on case's channel.
- lookup caching

## [6.12.2] - 2023-03-22
### Added
- Categories & Ordering for case types
- WYSIWYG widget, using `ngx-quill`

## [6.11.3] - 2023-03-15
### Changed
- allow negative numbers to `currency-widget`

## [6.11.2] - 2023-03-15
### Added
- Create new case side pane redesign
- Property `Lang` at `CasesUIOptions` for handling the attribute `<html lang='XXX'>` from the options.
> This change will require from the consumer api to handle the language. Eg:

```cs
app.UseCasesUI(options => {
        // ...
        options.Lang = "el"; 
    });
```

- onInit layout callback, now can return and set the entity to the ajsf form
- onInit layout callback, now it can access the `case.metadata` property
> example layout that sets the data.isLegal property 
```js
{ 
	"backoffice": [
		{
			// mv: modelValue, f: form (entity), md: case metadata
			"onInit": "function(mv,f,md) {f = f || {}; if(md.LegalEntity === '1' || md.LegalEntity.toLowerCase() === 'true') {f.isLegal = true;} return f; }",
		...
	]
}
```
## [6.10.6] - 2023-03-13
### Changed
- Redesigned the create case side panel. Removed the drop down for picking a case type. Now it displays all the available case types. Made the title dynamically change based on the wizard step. Minor design changes for picking customer number. Created a new component for displaying all the available case types.

##  [6.10.5] - 2023-03-02
### Changed
- Case type form for create/update is simplified. Only case type entity is being edited.

## [6.10.4] - 2023-03-01
### Changed
- `notifications` route can only be seen & navigated by non-admin users
- use `lib-toggle-button` component from `@indice/ng-components` npm package
### Fixed
- modal bugs introduced from `@indice/ng-components` npm package latest version

## [6.10.1] - 2023-02-15
### Added
- Dashboard with stats, filtered by role

## [6.9.1] - 2023-02-07
### Changed
- Notification Subscriptions are now per case type

## [6.6.0] - 2023-01-30
- Jumped to 6.6.0 to match other Indice Packages

## [6.5.1] - 2023-01-24
### Fixed
- Angular build

## [6.5.0] - 2023-01-24
### Added
- Support for net7
### Changed
- Changes following the API refactoring in dto names
- `JSON.stringify(data)` is no longer needed to send json data to a case. Case data request is dynamic.
- `JSON.parse(data)` is no longer needed for parsing. Case data response is dynamic.
### Removed
- Support for net3.1

## [6.4.3] - 2023-01-10
### Added
- add min/max date configuration for `date-widget` through layout options
- momentjs dependency

## [6.4.2] - 2023-01-10
### Added
- a lookup-selector widget
### Fixed
- a minor typeahead bug in lookup widget

## [6.4.0] - 2022-12-22
- Jumped to 6.4.0 to match other Indice Packages
