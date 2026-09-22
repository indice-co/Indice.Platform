# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [8.58.0] - 2026-09-22
### Changed (Bootstrap5 variant — visual redesign, breaking for CSS overrides)
- Every Bootstrap5 Razor page was rewritten against a new design system: Sass design tokens
  (`wwwroot/css/abstracts/_tokens.scss`) mapped onto Bootstrap 5.3 variables (`bootstrap.scss`), BEM-named
  components (`components/*`), one master layout with four "shells" (`auth`, `card`, `profile`, `article`)
  and four themes switched by a `data-theme` attribute on `<html>`. Page models, routes, form fields, handlers and
  validation are unchanged; only the markup and styles changed.
- **Breaking:** the legacy Sass partials (`_colors`, `_variables`, `_panel`, `_forms`, `_menu`, `_dropcard`, …)
  and their CSS classes (`.sign-in`, `.sign-up`, `.identity-panel`, `.hr-sect`, `.idp`, `.password-control`,
  `.reveal-icon`, `.drop-card`, `.wrapper`, `.footer`, …) are gone. Hosts that override
  `wwwroot/css/identity.css` or ship a `_custom.scss` written against the old class names must update them.
  The `_custom.scss` escape hatch is still imported last.
- **Breaking:** `IdentityUIOptions.HtmlBodyBackgroundCssClass` now defaults to an empty string (the selected
  template owns the page background); the `gradient-bg` / `image-bg` classes no longer exist.
- The header renders the brand and, for signed-in users, an avatar button that opens a slide-out account panel
  (profile, change/add password, grants, switch account, logout) on every viewport. The old inline navbar and
  dropdown card are gone.
- The footer now always shows the language switcher, copyright, legal links and a "made by" credit.
- `_StylesHead` preloads the two Gotham Greek faces the UI uses and no longer preconnects to
  `storage.googleapis.com`; the Open Sans Google Fonts declarations were removed (fonts are self-hosted).
- Stylesheet `<link>`s moved from `<body>` to `<head>`.
- `wwwroot/img/hero.jpg` (optimised, 1920 px) is the default hero/background image; override it by shipping
  your own file at the same path or by setting `--idui-hero-image` in a custom stylesheet.
### Added
- `IdentityUIOptions.Theme` (string, default `IdentityUIThemes.Split`) and the `IdentityUIThemes` constants `minimal`,
  `split`, `columns`, `panel`. Emitted as `data-theme` on `<html>`. The markup is identical for every theme: a theme is a
  block of `--idui-*` CSS custom property values under `[data-theme="name"]` (`wwwroot/css/themes/`), read by the
  layout engine in `wwwroot/css/layouts/_anonymous-shell.scss` and by every component. Hosts add a theme by choosing
  another name and shipping a stylesheet with those values; no recompilation needed. Bootstrap runtime variables
  (`--bs-*`) are bridged to the same properties in `bootstrap.scss`.
- `IdentityUIOptions.ShowMadeByCredit` (default `true`).
- `PageHeading` view component accepts an optional `subtitle`.
- `wwwroot/js/otp-field.js`: progressive enhancement that renders one-time codes as digit cells
  (`input.otp-field__input[data-otp-length]`); the original input stays in the form.
- New `IdentityUILocalizer` members / resource keys: `Hero_Title`, `Hero_Subtitle`, `Footer_MadeBy`,
  `Footer_LinksLabel`, `Login_Alternatively`, `Register_Subtitle`, `Nav_OpenMenu`, `Nav_CloseMenu`,
  `Account_SwitchAccount`, `Account_NavLabel`, `SkipToContent`, `Password_Show`, `Password_Hide`,
  `Mfa_ResendAvailableIn`, `Profile_Photo`, `Profile_PhotoUpload`, `Profile_PhotoHelp`,
  `Profile_PhotoConstraints`, `Profile_AccountDetails`, `Profile_DetailsSection` (English + Greek).
- Accessibility: skip link, `role="alert"` on validation summaries, `aria-describedby` from every control to its
  error message, real `<button aria-pressed>` for the password reveal, labelled offcanvas and navigation
  landmarks, visible focus ring on every interactive element.
### Fixed
- Two `Register_Phone_number]` typos in `Register.cshtml`.
- The dead `consent.js` reference on the consent page.

## [8.50.0] - 2026-06-22
### Added
- Added, enhanced and refined the layout of the transactional emails. The shared `_LayoutEmail` (both the
  Bootstrap5 and Tailwind variants) now renders a consistent, responsive footer with copyright, organization
  legal name & address, commercial registry number, support contact details and privacy/terms links.

  The organization details shown in the footer are **not populated by default**. To display the organization
  legal name, address, registry number and support contact, either add the relevant keys under the `General`
  section of your `appsettings.json`:

  ```json
  {
    "General": {
      "OrganizationLegalName": "Acme Ltd",
      "OrganizationAddress": "1 Example Street, City 12345",
      "OrganizationRegistryNumber": "123456789",
      "OrganizationSupportPhone": "+30 21 0000 0000",
      "OrganizationSupportEmail": "support@acme.example"
    }
  }
  ```

  or you can override the corresponding methods on `IdentityUILocalizer` (e.g. `OrganizationLegalName`,
  `OrganizationAddress`) to source the values from anywhere you like.
