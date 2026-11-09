# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
