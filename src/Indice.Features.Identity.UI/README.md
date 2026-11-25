# Indice.Features.Identity.UI

A comprehensive **Razor Class Library** that provides a complete, production-ready identity management UI for ASP.NET Core applications. This library delivers all the essential identity-related user interface components including authentication, registration, profile management, multi-factor authentication, and more.

## 🚀 Features

### Core Identity Operations
- **Authentication & Authorization**
  - Login with username/email and password
  - External provider authentication (Google, Facebook, Microsoft, etc.)
  - Multi-factor authentication (SMS, Email)
  - Remember me functionality
  - Logout and logout confirmation

- **User Registration & Onboarding**
  - Self-service user registration
  - Email confirmation workflow
  - Custom onboarding processes
  - Terms and conditions acceptance

- **Password Management**
  - Forgot password with email reset
  - Password reset confirmation
  - Password expiration handling
  - Change password functionality

- **Profile Management**
  - User profile editing (personal information, preferences)
  - Profile picture upload and management
  - Email address management (add, change, verify)
  - Phone number management (add, verify)
  - Timezone and locale preferences

- **Security Features**
  - Multi-factor authentication onboarding
  - Device/browser verification
  - Security notifications via email
  - Session management and grants review

### UI Framework Support
- **Bootstrap 5** - Complete responsive UI components
- **Customizable Themes** - Easy branding and customization

### Additional Features
- **Localization** - Multi-language support
- **Email Templates** - Pre-built email templates for all workflows
- **Error Handling** - User-friendly error pages (40X, 500, etc.)
- **Accessibility** - WCAG compliant UI components
- **Mobile Responsive** - Optimized for all device sizes

## 📦 Installation

```bash
dotnet add package Indice.Features.Identity.UI
```

## 🔧 Configuration

### Basic Setup

```csharp
public void ConfigureServices(IServiceCollection services)
{
  services.AddIdentity<User, Role>()
        .AddEntityFrameworkStores<IdentityDbContext>()
            .AddIdentityUI(options =>
       {
         // Basic configuration
       options.HomePageSlogan = "Welcome to our Digital Services Portal";
        options.CopyYear = 2024;
    options.EnableRegisterPage = true;
          options.EnableForgotPasswordPage = true;
          options.EnablePasswordConfirmation = false;
     options.AllowRememberLogin = true;
      });
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapRazorPages();
    });
}
```

### Advanced Configuration

```csharp
services.AddIdentityUI(options =>
{
    // Branding & Appearance
    options.HomePageSlogan = "Welcome to the {0} Digital Services <strong>Portal</strong>";
    options.AvatarColorHex = "1abc9c";
    options.EmailLinkColorHex = "1abc9c";
    options.Theme = IdentityUIThemes.Split;          // visual theme (data-theme on <html>)
    
    // Feature Toggles
    options.EnableLocalLogin = true;
    options.AutoProvisionExternalUsers = true;
    options.AutoAssociateExternalUsers = true;
    options.EnablePhoneNumberCallingCodes = false;
    // Show password confirmation fields on Register/PasswordExpired/ChangePassword/ForgotPasswordConfirmation.
    options.EnablePasswordConfirmation = true;
    options.AutomaticSigninAfterRegister = false;
    
    // File Upload Settings
    options.PictureUploadSizeLimit = 1024 * 1024 * 5; // 5MB
    options.PictureMaxSideSize = 512; // 512px
    
  // Session Management
    options.RememberMeLoginDuration = TimeSpan.FromDays(30);
    options.ShowLogoutPrompt = true;
    options.AutomaticRedirectAfterSignOut = false;
    // How long a passed MFA challenge before changing email, phone or username lasts (see "Profile MFA challenge").
    options.ProfileMfaChallengeLifetime = TimeSpan.FromMinutes(5);

    // Custom URLs
    options.TermsUrl = "https://example.com/terms";
    options.PrivacyUrl = "https://example.com/privacy";
  options.ContactUsUrl = "https://example.com/contact";
    
    // Custom Onboarding
    options.OnBoardingPage = "/CustomOnboarding/Welcome";
    
    // Homepage Services
    options.AddHomepageLink("Admin Panel", "~/admin", "admin-card", 
        visibilityPredicate: user => user.IsInRole("Admin"));
    options.AddHomepageLink("My Dashboard", "~/dashboard", "dashboard-card");
    
  // Event Handlers
    options.Events.OnUserRegistering = async context =>
    {
        // Custom logic during user registration
      context.User.CreatedBy = "System";
        await Task.CompletedTask;
    };
});
```

## 🎨 Customization

### UI framework

The package ships one page tree, `Pages/Bootstrap5`. The host picks the UI framework with the
`IdentityUIFrameworkVersion` MSBuild property (default `Bootstrap5`; any other value fails at startup). The Tailwind
page tree was removed in the next major release.

Each framework is a page tree under `Pages/{Framework}/` and a stylesheet folder under `wwwroot/css/{framework}/`.
The `{Framework}` folder (and the page group folders `Profile`, `Legal` and `Verification`) is removed from the view
path at startup, so all frameworks share the page models in `Pages/*.cs`, the page names and the host override paths.
To add a framework, add its value to the internal `UIFramework` enum and its page tree; Vite compiles every
`wwwroot/css/**/*.scss` entry file that does not start with `_`.


### Page groups and layouts (Bootstrap5)

The Bootstrap5 pages are grouped in folders, and every page sets its group's layout, which nests in
`Shared/_IdentityLayout.cshtml` (document, header, footer). The group folder only organises the source: it is removed
from the view path like the framework folder, so `/Pages/Bootstrap5/Auth/Login.cshtml` is still the page `/Login`
(same routes, `RedirectToPage` names and host overrides).

| Folder | Layout | Shell (`<body>` class) | Pages |
|---|---|---|---|
| `Auth/` | `_AuthLayout` | `card` (one centered `.auth-card`); `auth` (hero aside + form) on Login, Register | Sign in/up, MFA and onboarding, verification, forgot password, consent, external login, logout, errors |
| `Profile/` | `_ProfileLayout` | `profile` (image header band, `_ProfileNav` sidebar, `.data-card` stack) | Profile, Security, Change/Add password, Grants |
| `Home/` | `_HomeLayout` | `card` | Home |
| `Article/` | `_ArticleLayout` | `article` (wide `.article-card` with rendered markdown) | Terms, Privacy, Accept terms |

The group layouts pass the page's optional `meta`, `css`, `hero` and `scripts` sections through to `_IdentityLayout`.
A host can replace a group layout by shipping e.g. `Pages/Shared/_AuthLayout.cshtml`, or the outer frame with
`Pages/Shared/_IdentityLayout.cshtml`. Pages that set no group layout (host pages, the `Redirect` view) get the `card` shell.

### Styling

- Each UI framework has its own stylesheet folder. Bootstrap5 uses `wwwroot/css/bootstrap5/` (`bootstrap.bs.scss` +
  `identity.bs.scss` → `bootstrap.bs.css` + `identity.bs.css`). The paths below are relative to `wwwroot/css/bootstrap5/`.
- Design tokens live in `abstracts/_tokens.scss` (colours, fluid type scale, spacing, radii, shadows,
  layout widths). They are mapped onto Bootstrap's Sass variables in `bootstrap.bs.scss` and exposed as
  `--idui-*` custom properties for runtime tweaks.
- Custom components use BEM (`.auth-card__title`, `.field__error`, `.provider-btn--microsoft`, …) and live in
  `components/`. Themes live in `themes/` (values only), the layout engine and shells in `layouts/`.
- Stylesheets are compiled by Vite (`npm run build`, also run by `dotnet build`); the compiled
  `wwwroot/css/**/*.css` files are committed.
- Hosts override styles by shipping `wwwroot/css/bootstrap5/identity.bs.css` (replaces the stylesheet) or by adding rules to a
  `wwwroot/css/bootstrap5/_custom.scss` that the package imports last when the SCSS imports feature is enabled.
- The hero image is set per theme with `--idui-hero-image` (SVG artwork in `wwwroot/img/backgrounds/`). Set
  `--idui-hero-image` (and `--idui-hero-overlay` / `--idui-hero-overlay-strong` for a photo) on the same selector as the
  theme to replace it. `wwwroot/img/hero.jpg` is still shipped for hosts that reference it.

#### Palettes

Every built-in theme carries its own palette, declared on `:root[data-theme='name']` in `themes/`:

| Theme | Palette | Primary | Hero artwork |
|-------|---------|---------|--------------|
| `split` (default) | Aurora | `#6d28d9` violet | `backgrounds/aurora.svg` (northern lights) |
| `minimal` | Mint | `#047857` emerald | `backgrounds/mint.svg` (profile header band; soft green mesh page) |
| `columns` | Sunset | `#c2410c` burnt orange | `backgrounds/sunset.svg` (plum sky, sun and dunes) |
| `panel` | Ocean | `#1d4ed8` cobalt | `backgrounds/ocean.svg` (topographic waves; deep blue page) |

- `--idui-color-primary` / `-primary-hover` / `-on-primary` drive actions, links, selection and focus (buttons,
  checkboxes, active navigation, icon chips). `--idui-color-ink` stays for headings and strong text.
- `--idui-color-primary-soft`, `-primary-soft-hover` and `-primary-border` are derived from the primary with
  `color-mix()`, as are `--idui-color-focus`, `--idui-shadow-focus` and `--idui-shadow-primary`, so overriding the
  primary re-tints them.
- Aurora is also the `:root` default, so a host can re-colour the split theme from `:root`. The other themes set their
  palette on `:root[data-theme='name']`; override it on the same selector, e.g.
  `:root[data-theme='panel'] { --idui-color-primary: #0f766e; --idui-color-primary-hover: #115e59; }`.

#### Icons

Icons are [Lucide](https://lucide.dev) (ISC), served as one SVG sprite. Write `<icon name="shield-check" />` (tag helper,
`TagHelpers/IconTagHelper.cs`); it renders `<svg class="icon"><use href="~/lib/lucide/sprite.svg#shield-check"></use></svg>`
sized to `1em` in `currentColor`. Add `aria-label` for a meaningful icon; otherwise it is `aria-hidden`.

- `npm run build` writes `wwwroot/lib/lucide/sprite.svg` with only the icons used by `Pages/**/*.cshtml` and
  `IdentityUIIcons.cs`; an unknown name fails the build. Dynamic icons must be listed in `IdentityUIIcons.cs`.
- `IdentityUIIcons.ForAuthenticationMethod(code)` maps MFA methods to icons; `IdentityUIIcons.GetSpriteHref(ViewContext)`
  returns the versioned sprite URL for client-side bindings.
- Host pages that need other icons can ship their own sprite or inline SVG; the `.icon` class styles any inline SVG.
- Stroke width is `--idui-icon-stroke-width` (default `2`).

A typical form field:

```html
<div class="field">
    <label asp-for="Input.Email" class="field__label">Email <span class="field__required" aria-hidden="true">*</span></label>
    <input class="form-control field__control" asp-for="Input.Email" aria-describedby="Input_Email-error" />
    <span asp-validation-for="Input.Email" class="field__error" id="Input_Email-error"></span>
</div>
```

### Overriding Static Assets

You can override any static asset by placing a file with the same path in your host application's `wwwroot`:

```
wwwroot/
├── css/
│   └── bootstrap5/
│       └── identity.bs.css   # Overrides the library's Bootstrap5 identity stylesheet
├── js/
│   └── app.js       # Additional JavaScript
└── images/
    └── logo.png        # Custom logo
```

### Upgrading to 8.58

8.58.0 moved the stylesheets into one folder per UI framework. The compiled CSS is unchanged; only the paths moved.

| Before | Now |
|---|---|
| `wwwroot/css/bootstrap.css` | `wwwroot/css/bootstrap5/bootstrap.bs.css` |
| `wwwroot/css/identity.css` | `wwwroot/css/bootstrap5/identity.bs.css` |
| `wwwroot/css/identity.tw.css` | `wwwroot/css/tailwind/identity.tw.css` |
| `wwwroot/css/_custom.scss` | `wwwroot/css/bootstrap5/_custom.scss` |

Check your host for:

- **Overridden stylesheets**: a file at one of the old paths in your `wwwroot` is still served but no longer linked,
  so it silently stops applying. Move it to the new path.
- **Direct links**: layouts or partials copied from the package, a custom `_Styles` partial, or other apps that
  link the IdP stylesheets get a 404 until the `href` is updated.
- **SCSS**: a `_custom.scss` must move to `wwwroot/css/bootstrap5/`. Builds that compile or `@use` the package's
  SCSS sources must use the new entry files (`bootstrap5/*.bs.scss`; 8.58 also had `tailwind/identity.tw.scss`, since removed).

Hosts that use the package pages and layouts without overriding stylesheets need no changes. The Bootstrap5 variant
was also redesigned in 8.58.0 (new markup and class names); see the CHANGELOG for those breaking changes.

### Custom Page Templates

Override specific pages by creating them in your host application:

1. On the same path as in the library but ommiting the `UIFramework` folder and, for Bootstrap5, the page group folder: `/Pages/Bootstrap5/Auth/Login` will become `/Pages/Login`.
```
Pages/
├── Login.cshtml     # Custom login page
├── Register.cshtml  # Custom registration page
```
2. Under a `Themes` folder to create a conditional version for a spesific client. 
```
Pages/
├── Themes/
│   ├── MyApplicationClientLogin.cshtml     # Custom login page
│   └── MyApplicationClientRegister.cshtml  # Custom registration page
```
In case (2) you will also need to set the client_id by creating the corresponding PageModel and decorating it with the `[IdentityUIClient("MyApplicationClient")]` attribute.


### Email Template Customization

Email templates can be customized by placing files in your application:

```
Pages/
├── Shared/
│   ├── EmailConfirmYourEmail.cshtml
│   ├── EmailForgotPassword.cshtml
│   └── EmailSecurityNotification.cshtml
```

## 🏗️ Architecture

### Project Structure

```
Indice.Features.Identity.UI/
├── Models/      # View models and input models
├── Pages/              # Razor pages organized by theme
│   ├── Bootstrap5/         # Bootstrap 5 theme: Auth/, Profile/, Home/, Article/ page groups + Shared/ layouts and partials
│   └── Shared/         # Shared layouts and components
├── Validators/  # FluentValidation validators
├── ViewComponents/     # Reusable view components
├── TagHelpers/      # Custom tag helpers
├── EventHandlers/        # Event handling for notifications
├── Localization/     # Localization resources
├── Filters/            # Custom action filters
└── wwwroot/    # Static web assets
    ├── css/       # Compiled stylesheets
 ├── js/     # JavaScript libraries
    ├── lib/ # Third-party libraries
    └── images/           # Images and icons
```

### Key Components

- **Page Models**: Inherit from `BasePageModel` for common functionality
- **Input Models**: Data transfer objects with validation attributes
- **Validators**: FluentValidation rules for form validation
- **View Components**: Reusable UI components (ProfileSidebar, PageHeading, etc.)
- **Tag Helpers**: Custom HTML helpers (ProfilePictureImageTagHelper, IconTagHelper)
- **Event Handlers**: Extensible event system for custom business logic

### Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Indice.Features.Identity.Core | Latest | Core identity functionality |
| FluentValidation.AspNetCore | 11.3.1 | Form validation |
| HtmlAgilityPack | 1.12.3 | HTML parsing and manipulation |
| Bootstrap | 5.3.6 | UI framework |
| jQuery | 3.7.1 | JavaScript functionality |
| Lucide (`lucide-static`) | 1.80.0 | Icons (build-time SVG sprite) |

## 🌐 Available Pages

### Authentication Pages
- `/Login` - User login with external providers
- `/Logout` - Logout confirmation and processing
- `/LoggedOut` - Post-logout confirmation

### Registration & Recovery
- `/Register` - User registration form
- `/ForgotPassword` - Password reset request
- `/ForgotPasswordConfirmation` - Password reset email sent confirmation
- `/ConfirmEmail` - Email address confirmation
- `/ConfirmEmailChange` - Email change confirmation

### Profile Management
- `/Profile` - User profile management dashboard (read only; each section links to its edit page)
- `/ChangeProfileDetails` (`/manage/profile/details`) - Edit profile details and user editable claims
- `/ChangePreferences` (`/manage/preferences`) - Edit the preferred language
- `/Security` (`/manage/security`) - Sign-in methods overview (password, email, phone, two-factor authentication)
- `/ChangePassword` - Password change form
- `/AddEmail` - Add additional email address
- `/AddPhone` - Add phone number
- `/VerifyPhone` - Phone number verification

#### User editable claims
`/ChangeProfileDetails` shows the well known profile fields (given/family name, TIN, birth date, time zone, commercial
consent) and every other claim type marked **User editable** in the admin (`ClaimType.UserEditable`), except auto
generated claim types and claims managed by other pages (email, phone, locale, picture, etc.). The input follows the
claim type's value type (`String`, `Int`, `DateTime`, `Boolean`); `Required` and `Rule` (a regular expression that must
match the whole value) are validated on the server. Only changed values are saved. Given name, family name and TIN are
read only unless their claim types are user editable. The same claims are listed, read only, on `/Profile`.

#### Profile MFA challenge
When a user with MFA enabled changes their email (`/ChangeEmail`, `/manage/email`), phone number (`/ChangePhone`,
`/manage/phone`) or username (`/ChangeUserName`, `/manage/username`), an inline modal first asks for a code from one of
the user's code-based MFA methods (SMS, Viber, email or authenticator app). Another method can be picked only when
`AllowDowngradeAuthenticationMethod` is enabled, as on `/Mfa`. A passed check is kept in a short-lived cookie
(`.mfachallenge`) bound to the user and the login session, lasts `ProfileMfaChallengeLifetime` (5 minutes) and covers
email, phone and username. Wrong codes count towards lockout. Users with MFA
but no code-based method are blocked; users without MFA are not affected. Requests are limited by the
`manage/mfa-challenge` rate limiter policy. Replace `IProfileMfaChallengeService` to customise the rules. Page overrides must
render `<partial name="_ProfileMfaChallenge" model="Model.MfaChallenge" />`. The username page is unavailable when
`DisableEmailEdit` is set or the email is used as the username (`IdentityOptions:User:EmailAsUserName`).

#### Security page
`/Security` (`/manage/security`) has its own *Security* menu item. It shows the user's avatar, name and last login, and
a *Sign-in methods* list. Each row (password, email, phone, two-factor authentication) shows the value and its status
and expands to show details and a button to the existing page: `/ChangePassword` or `/AddPassword`, `/ChangeEmail`,
`/ChangePhone` and `/ManageMfa`. The email, phone and two-factor buttons are hidden when `DisableEmailEdit`,
`DisablePhoneEdit` or `DisableMfaEdit` is set. The password and MFA pages return to `/Security`.

#### Profile MFA management
`/ManageMfa` (`/manage/mfa`) is linked from the *Two-factor authentication* row of `/Security`. Users can turn MFA on,
change the preferred method (SMS on a confirmed phone, email on a confirmed email, or authenticator app), set up a new
authenticator app (the new key is stored only after a valid code) and regenerate recovery codes (authenticator app only).
Turning MFA off or resetting it is not offered; those remain admin actions. When MFA is already on, every change needs
the profile MFA challenge. Moving from the authenticator app to SMS or email removes the authenticator key and its
recovery codes. The preferred method only chooses between methods of the same security level, so the authenticator app
would otherwise still be used. Set `IdentityUIOptions.DisableMfaEdit` to hide the page.

### Multi-Factor Authentication
- `/Mfa` - MFA challenge page
- `/MfaOnboarding` - MFA setup wizard
- `/MfaOnboardingAddPhone` - Add phone for MFA
- `/MfaOnboardingVerifyPhone` - Verify phone for MFA
- `/MfaOnboardingAddEmail` - Add email for MFA
- `/MfaOnboardingVerifyEmail` - Verify email for MFA

### Security & Consent
- `/Consent` - OAuth consent page
- `/Grants` - Review active grants and sessions
- `/AcceptTerms` - Terms and conditions acceptance
- `/Associate` - External account association

### Utility Pages
- `/Home` - Landing page with service links
- `/Error` - General error page
- `/Error40X` - 404/403 error pages
- `/Privacy` - Privacy policy page
- `/Terms` - Terms and conditions page
- `/Challenge` - External authentication challenge

## 🛠️ Build Process

The library includes automated build processes for static assets:

```json
{
  "scripts": {
    "gulp": "gulp",
    "npm:install": "npm install"
  }
}
```

### Build Targets
- **NpmInstall**: Installs Node.js dependencies
- **CopyLibs**: Copies third-party libraries to wwwroot
- **Sass**: Compiles SCSS stylesheets

### Asset Management
- **Fingerprinting**: Automatic asset versioning for cache busting
- **Minification**: JavaScript and CSS optimization
- **Image Optimization**: Automatic image compression

## 📚 Examples

### Custom Registration Event

```csharp
services.AddIdentityUI(options =>
{
    options.Events.OnUserRegistering = async context =>
    {
        var user = context.User;
    var httpContext = context.HttpContext;
        
    // Set default user properties
     user.Department = "General";
        user.IsActive = true;
        user.CreatedDate = DateTime.UtcNow;
        
        // Log registration attempt
        var logger = httpContext.RequestServices.GetService<ILogger<Startup>>();
        logger.LogInformation("User {Email} is registering", user.Email);
        
        await Task.CompletedTask;
    };
});
```

### Custom Homepage Services

```csharp
services.AddIdentityUI(options =>
{
    options.AddHomepageLink("HR Portal", "https://hr.company.com", "hr-card", 
        imageSrc: "/images/hr-icon.png",
        visibilityPredicate: user => user.HasClaim("department", "HR"));
        
    options.AddHomepageLink("Finance Dashboard", "~/finance", "finance-card",
        visibilityPredicate: user => user.IsInRole("Finance"));
});
```

## 🤝 Contributing

This library is part of the Indice Platform ecosystem. For contributions, please refer to the main platform repository guidelines.

## 📄 License

This project is licensed under the MIT License - see the main platform repository for details.

## 🔗 Related Packages

- **Indice.Features.Identity.Core** - Core identity functionality
- **Indice.Features.Identity.Server** - IdentityServer4 integration
- **Indice.Features.Identity.AdminUI** - Administrative interface
- **Indice.AspNetCore** - Core ASP.NET Core utilities

---

For more information and advanced scenarios, please refer to the [Indice Platform Documentation](https://github.com/indice-co/Indice.Platform)
