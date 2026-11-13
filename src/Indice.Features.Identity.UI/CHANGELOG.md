# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased] - next major
### Added (profile MFA challenge, breaking)
- **Profile MFA challenge (SCA):** users with MFA enabled must pass an MFA check before changing their email, phone
  number or username on `/ChangeEmail` (`/manage/email`), `/ChangePhone` (`/manage/phone`) and `/ChangeUserName`
  (`/manage/username`). The check is an inline modal (Bootstrap5 and Tailwind,
  partial `Shared/_ProfileMfaChallenge`). It uses the user's code-based methods (SMS, Viber, email, authenticator app).
  The user can pick another method only when `AllowDowngradeAuthenticationMethod` is enabled, the same rule as `/Mfa`.
  Codes are sent only when the user clicks *Send code*. Push (trusted device), FIDO2 and recovery codes are not
  supported yet. Users with MFA but no code-based method are blocked from changing email, phone and username; users
  without MFA are not affected.
- A passed check is stored in a new short-lived cookie (`.mfachallenge`, scheme
  `ExtendedIdentityConstants.ProfileMfaChallengeScheme`). It covers email, phone and username, is bound to the user (`sub`)
  and the login session (`sid`), and lasts `IdentityUIOptions.ProfileMfaChallengeLifetime` (default 5 minutes, absolute).
  Wrong codes count towards account lockout; a locked-out user is signed out. Signing out clears the cookie.
- New service `IProfileMfaChallengeService` (default `ProfileMfaChallengeService`), registered by `AddIdentityUI` with
  `TryAddScoped` so hosts can replace it.
- New rate limiter policy `manage/mfa-challenge` (default 10 POST requests per minute). Hosts with an explicit
  `IdentityServer:RateLimiter:Rules` list can add it to tune the limit; when missing, the default is used.
- New labels `ProfileMfaChallenge_*` (English and Greek; other languages fall back to English).
- New **Change username** page `/ChangeUserName` (`/manage/username`, `BaseChangeUserNameModel`, input
  `ChangeUserNameInputModel` with `ChangeUserNameInputModelValidator`), styled like *Change email* and gated by the MFA
  challenge. It is unavailable (redirects to `/Profile`) when `DisableEmailEdit` is set or
  `IdentityOptions:User:EmailAsUserName` is enabled. New labels `ChangeUserName_PageTitle`, `ChangeUserName_Description`
  and `ChangeUserName_Succeeded`.
### Changed
- **Breaking:** the inline username editor on `/Profile` is replaced by a link to `/ChangeUserName`. The
  `BaseProfileModel.OnPostUpdateUserNameAsync` handler, the `AccountFieldEmail` / `AccountFieldPhone` /
  `AccountFieldUserName` constants and `ProfileViewModel.EditingField` are removed, together with the unused
  `account-row__edit`, `account-row__actions` and `account-row--editing` styles. Profile page overrides that post the
  `UpdateUserName` handler must link to `/ChangeUserName` instead.
- `ProfileMfaChallenge_Prompt` and `ProfileMfaChallenge_Blocked` now refer to "account details" instead of email and phone.
- **Breaking:** `BaseChangeEmailModel` and `BaseChangePhoneModel` now inherit `BaseProfileMfaChallengePageModel`
  (the `UserManager` property is inherited from it). Their `OnGetAsync`, `OnPostAsync` and `OnPostVerifyAsync`
  handlers return the page with the MFA challenge modal while a check is needed. Hosts that override these pages must render
  `<partial name="_ProfileMfaChallenge" model="Model.MfaChallenge" />` and post the `MfaChallengeSend` / `MfaChallengeSelect` /
  `MfaChallengeVerify` handlers, otherwise the user cannot change email or phone.
### Known limitations
- The `/api/my/account/email`, `/api/my/account/phone-number` and `/api/my/account/username` endpoints are not covered
  by the MFA challenge.

## [8.XX.0] - 2026-09-22
### Changed (Bootstrap5 and Tailwind — stylesheet locations, breaking for hosts that reference them)
- **Breaking:** stylesheets are split per UI framework into `wwwroot/css/bootstrap5/` and `wwwroot/css/tailwind/`,
  and nothing is shared between the two trees. The compiled CSS is unchanged; only the file locations moved:

  | Before | Now |
  |---|---|
  | `~/css/bootstrap.css` | `~/css/bootstrap5/bootstrap.bs.css` |
  | `~/css/identity.css` | `~/css/bootstrap5/identity.bs.css` |
  | `~/css/identity.tw.css` | `~/css/tailwind/identity.tw.css` |
  | `wwwroot/css/_custom.scss` | `wwwroot/css/bootstrap5/_custom.scss` |

  Hosts that **override** one of these files by shipping it at the old path must move it: the old file is still
  served but no page links it any more, so the override is silently ignored. Hosts that **link** the old URLs
  (layouts copied from the package, a custom `_Styles` partial, other apps) get a 404. Hosts that use the package
  pages and layouts as-is need no changes. See *Upgrading to 8.58* in the README.
- SCSS sources moved with them (`bootstrap.scss` → `bootstrap5/bootstrap.bs.scss`, `identity.scss` →
  `bootstrap5/identity.bs.scss`, `identity.tw.scss` / `_reveal.scss` → `tailwind/`), and the package's Sass build no
  longer puts `wwwroot/css` on the load path. The Tailwind build no longer scans the Bootstrap5 sources.
### Changed (Bootstrap5 variant — visual redesign, breaking for CSS overrides)
- Every Bootstrap5 Razor page was rewritten against a new design system: Sass design tokens
  (`wwwroot/css/bootstrap5/abstracts/_tokens.scss`) mapped onto Bootstrap 5.3 variables (`bootstrap.bs.scss`), BEM-named
  components (`components/*`), one master layout with four "shells" (`auth`, `card`, `profile`, `article`)
  and four themes switched by a `data-theme` attribute on `<html>`. Page models, routes, form fields, handlers and
  validation are unchanged; only the markup and styles changed.
- Bootstrap5 pages are grouped in `Pages/Bootstrap5/Auth/`, `Profile/`, `Home/` and `Article/`, each with a layout
  (`_AuthLayout`, `_ProfileLayout`, `_HomeLayout`, `_ArticleLayout`) nested in `_IdentityLayout`. The group folder is
  removed from the view path like the framework folder, so routes, page names (`/Login`), `RedirectToPage` calls and
  host override paths (`Pages/Login.cshtml`) are unchanged.
- **Breaking:** Bootstrap5 pages set their group layout themselves, so a layout chosen in the host's `_ViewStart` no
  longer applies to them; override `Pages/Shared/_IdentityLayout.cshtml` or a group layout instead. The profile
  sub-navigation moved from `_IdentityLayout` to `_ProfileLayout`, so a host `_IdentityLayout` override no longer needs
  to render it.
- **Breaking:** the legacy Sass partials (`_colors`, `_variables`, `_panel`, `_forms`, `_menu`, `_dropcard`, …)
  and their CSS classes (`.sign-in`, `.sign-up`, `.identity-panel`, `.hr-sect`, `.idp`, `.password-control`,
  `.reveal-icon`, `.drop-card`, `.wrapper`, `.footer`, …) are gone. Hosts that override
  `wwwroot/css/identity.css` or ship a `_custom.scss` written against the old class names must update them.
  The `_custom.scss` escape hatch is still imported last (now `wwwroot/css/bootstrap5/_custom.scss`).
- **Breaking:** `IdentityUIOptions.HtmlBodyBackgroundCssClass` now defaults to an empty string (the selected
  template owns the page background); the `gradient-bg` / `image-bg` classes no longer exist.
- The header renders the brand and, for signed-in users, an avatar button that opens a slide-out account panel
  (profile, change/add password, grants, switch account, logout) on every viewport. The old inline navbar and
  dropdown card are gone.
- The footer now always shows the language switcher, copyright, legal links and a "made by" credit.
- `_StylesHead` preloads the two Gotham Greek faces the UI uses and keeps the `storage.googleapis.com` preconnect;
  the Open Sans Google Fonts declarations were removed (fonts are self-hosted).
- The header shows only the logo (the application name is its accessible label, not visible text).
- Themes apply the hero background to every non-profile page whether or not the user is signed in (Home, Consent,
  Logout and Error keep the image after sign-in).
- Register and Profile show the timezone select (`Input.ZoneInfo`); Register pre-selects the browser's zone through
  `js/zone-info-select.js`, as on Tailwind.
- Disabled and read-only controls render muted; `$link-hover-decoration` is `null`, so navigation links are not
  underlined on hover.
- Stylesheet `<link>`s moved from `<body>` to `<head>`.
- `wwwroot/img/hero.jpg` (optimised, 1920 px) is the default hero/background image; override it by shipping
  your own file at the same path or by setting `--idui-hero-image` in a custom stylesheet.
### Added
- `IdentityUIOptions.Theme` (string, default `IdentityUIThemes.Split`) and the `IdentityUIThemes` constants `minimal`,
  `split`, `columns`, `panel`. Emitted as `data-theme` on `<html>`. The markup is identical for every theme: a theme is a
  block of `--idui-*` CSS custom property values under `[data-theme="name"]` (`wwwroot/css/bootstrap5/themes/`), read by the
  layout engine in `wwwroot/css/bootstrap5/layouts/_anonymous-shell.scss` and by every component. Hosts add a theme by choosing
  another name and shipping a stylesheet with those values; no recompilation needed. Bootstrap runtime variables
  (`--bs-*`) are bridged to the same properties in `bootstrap.bs.scss`.
- `IdentityUIOptions.ShowMadeByCredit` (default `true`).
- `PageHeading` view component accepts an optional `subtitle`.
- `wwwroot/js/otp-field.js`: progressive enhancement that renders one-time codes as digit cells
  (`input.otp-field__input[data-otp-length]`); the original input stays in the form.
- New `IdentityUILocalizer` members / resource keys: `Hero_Title`, `Hero_Subtitle`, `Footer_MadeBy`,
  `Footer_LinksLabel`, `Register_Subtitle`, `Nav_OpenMenu`, `Nav_CloseMenu`,
  `Account_SwitchAccount`, `Account_NavLabel`, `SkipToContent`, `Password_Show`, `Password_Hide`,
  `Mfa_ResendAvailableIn`, `Profile_Photo`, `Profile_PhotoUpload`, `Profile_PhotoHelp`,
  `Profile_PhotoConstraints`, `Profile_AccountDetails`, `Profile_DetailsSection` (all shipped languages: en, el, de, es, fr, it, ja, pt).
- Accessibility: skip link, `role="alert"` on validation summaries, `aria-describedby` from every control to its
  error message, real `<button aria-pressed>` for the password reveal, labelled offcanvas and navigation
  landmarks, visible focus ring on every interactive element.
### Fixed
- Two `Register_Phone_number]` typos in `Register.cshtml`.
- The dead `consent.js` reference on the consent page.
## [8.59.0]
### Added
- Added support for selecting the captcha provider through `appsettings.json` via `Recaptcha:Provider`.

  Configure captcha under the `Recaptcha` section:

  ```json
  {
    "Recaptcha": {
      "Provider": "Recaptcha",
      "SiteKey": "your-v3-site-key",
      "SecretKey": "your-v3-secret-key",
      "SiteKeyV2": "your-v2-site-key",
      "SecretKeyV2": "your-v2-secret-key",
      "ScoreThreshold": 0.5,
      "EnabledInLoginPage": true
    }
  }
  ```

  Supported values for `Provider` are:
  - `None`
  - `Recaptcha`
  - `HCaptcha`


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
