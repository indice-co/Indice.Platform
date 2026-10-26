# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
- The case details page shows its content in tabs: the case data form, the case attachments as a flat list (open / download),
  the data versions (paged and searchable by author, each version rendered read-only with the case type layout) and the related cases (when there are any).
- Case data form renderers for what the JSON Forms angular-material set supports and the Cases form did not:
  nested objects (`Control` on an object property, so schemas without a UI schema render at any depth), `Label` elements,
  `options.multi` (textarea), `options.toggle`, `options.slider`, `format: date-time` / `time`, arrays of enum values (checkboxes),
  `oneOf` / `anyOf` / `allOf`, the `ListWithDetail` element, and `options.table: true` (with optional `options.columns`) for arrays.
  Elements no renderer accepts show their value as JSON instead of "No applicable renderer found".
- `imageUri` widget (`options.widget: "imageUri"`) for string fields: the image is picked from a file (`image/*`, stored as a
  `data:` uri, up to `options.maxSize` bytes, 1 MB by default) or given as an external `https://` url, with a preview;
  read-only views show the image. Translation keys: `jsonforms.imageUri.*`.
- Read-only cases
- When the user cannot open cases, the cases list shows an info button where the "New case" button would be, explaining why.
- `CasesUIOptions.DashboardChannels`: the dashboard shows a "by case type" and a "by status" report for each listed case
  channel, a row per channel. Defaults to `Managed` and `External`; an empty list shows none. These reports follow
  `DashboardTags` through `GroupedByCasetype` / `GroupedByStatus`. A channel is labelled by its `channels.<name>`
  translation when there is one, otherwise by its name.
- A case type is created in a four-step wizard: general → schema, layout & translations → configuration → checkpoint types.
  "Save and continue" on the configuration step creates the case type, and the last step lists its checkpoint types to add.
  The schema and layout are edited side by side, with the translations below them. Editing a case type shows the same
  parts as tabs (checkpoint types included), with a red dot on tabs that have errors.
- Case type translations are edited in forms instead of JSON (saved in the same `translations` / `layoutTranslations` format):
  - title and description per app language; a language with a description but no title cannot be saved, since the server
    would show it an empty title;
  - form texts: every `title`, `placeholder`, `enumNames` and `helpvalue` text of the schema and layout (the properties the
    server translates) with its translation, with progress, search, "untranslated" / "unused" filters and removal of
    translations whose text is gone. A warning lists the `label`s of a JSON Forms UI schema layout, which the server does not translate.
- The checkpoint type create / edit pages take a `returnUrl` query parameter; opened from a case type (wizard or edit page),
  they return to its checkpoint types.
### Changed
- On a case type's list, "New case" is shown only when that case type can be created, and opens with it already chosen.
- The channel reports replace the fixed Agent/Customer (branch/eBanking) dashboard reports, so the `Agent*` / `Customer*`
  entries of `DashboardTags` no longer select a report; to hide the channel reports set `DashboardChannels` to an empty list.
  Translation keys: `dashboard.agentGroupedByCasetype`, `dashboard.customerGroupedByCasetype`, `dashboard.agentGroupedByStatus`
  and `dashboard.customerGroupedByStatus` are replaced by `dashboard.channelGroupedByCasetype` / `dashboard.channelGroupedByStatus`
  (with a `{{channel}}` parameter) and `channels.Managed` / `channels.External`.
- The `case-channel` UI schema option treats the legacy `Agent` / `Customer` channel names as `Managed` / `External`.
- The JSON fields of the case type form (schema, layout, translations, configs) use a code editor (CodeMirror) instead of
  plain textareas: syntax highlighting, line numbers, bracket matching, folding and inline JSON parse errors.
- Empty case type translations are no longer saved (the server would replace the text with an empty string).
  Translation keys: `caseTypes.form.translations`, `caseTypes.form.layoutTranslations`, `caseTypes.form.sectionData` and
  `caseTypes.form.fixJson` are removed; `caseTypes.steps.*`, `caseTypes.translations.*`,
  `caseTypes.form.schemaHint`, `caseTypes.form.layoutHint`, `caseTypes.edit.fixTabs`,
  `common.steps`, `common.stepState.*`, `common.stepOf`, `common.hasErrors`, `common.next` and `common.done` are added.
### Fixed
- Editable checkboxes in the case data form could not be ticked.
- Side panes (new case, saved queries) did not scroll, so the new-case pane cut off the case type list when there were many.
  The pane body now scrolls under a fixed header and footer, with a thin scrollbar.

## [8.50.0] - 2026-06-16

### Overriding / extending translations

Default implementation covers all the previous el.json keys and more (previously hardcoded UI html,ts etc), but if you want to override any of them or add your own,
you can layer a second resource onto the same `cases-i18n` endpoint with `AddTranslationsResource`.

The keys for the new resources are the same as the previous keys in `el.json` but with dots instead of camelCase,
and prefixed with the page name. For example, the key `groupedByCasetype` in `el.json` becomes `dashboard.groupedByCasetype` in the new resource file.

**Note**: You can find the rest of the keys in [Indice.Features.Cases.Server project, inside the Resources folder.](https://github.com/indice-co/Indice.Platform/tree/develop/src/Indice.Features.Cases.Server/Resources)

Previous keys in `el.json` example:
```json
{
  "dashboard": {
    "groupedByCasetype": "Συνολικές Υποθέσεις ανά Τύπο Υπόθεσης"
    }
}
```
New resource en/neutral example:
```xml
  <data name="dashboard.groupedByCasetype" xml:space="preserve">
    <value>Total Cases by Case Type</value>
  </data>
```
New resource el example:
```xml
  <data name="dashboard.groupedByCasetype" xml:space="preserve">
    <value>Συνολικές Υποθέσεις ανά Τύπο Υπόθεσης</value>
  </data>
```

### Example override:
```csharp
// resourceName: "MyApp.Cases.Translations" -> look for MyApp.Cases.Translations.{culture}.resx embedded resources * inside a Resources folder * (by default)
// endpointRoutePattern: null -> reuse the default cases-i18n endpoint
// resourceLocation: "MyApp.Host" -> look for embedded resources in the MyApp.Host assembly
builder.Services.AddTranslationsResource( resourceName: "MyApp.Cases.Translations",
    endpointRoutePattern: null, resourceLocation: "MyApp.Host");
```

**Make the added resource actually resolve.** All resources sharing the
endpoint are resolved in one pass; if any one can't find its embedded manifest, the entire request
throws `MissingManifestResourceException`. For your resource to resolve:

- Provide a **neutral** `.resx` (e.g. `MyApp.Cases.Translations.resx`), not only culture-specific ones.
- Mark the `.resx` files as **`EmbeddedResource`**.
- With `ResourcesPath = "Resources"` (the platform default), place them under a **`Resources`** folder so
  the manifest name is `{Assembly}.Resources.{baseName}`. Pass the base name *without* the `Resources.`
  prefix — the `ResourcesPath` adds it.
- Pass the correct **assembly name** as `resourceLocation`, and ensure that assembly's `RootNamespace`
  matches its assembly name.

**Note**: You can remove the previous translation file `el.json` as it is no longer needed.

### Disabling the new endpoints.
By default, the system will automatically map the built-in translation endpoints.<br>
However, if you have integrated your own custom translation service and wish to bypass the provided endpoints entirely, 
you can easily opt out of this default behavior. Simply update your startup configuration by setting `CaseServerOptions.MapTranslations` to false
```csharp
builder.AddCaseServer(options => { options.PathPrefix = "/api"; // rest of configs...
                                   options.MapTranslations = false; });
```

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
