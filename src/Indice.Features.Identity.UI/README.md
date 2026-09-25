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
- **Tailwind CSS** - Modern utility-first styling
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

Two page trees ship in the package: `Pages/Bootstrap5` (default) and `Pages/Tailwind`. The host picks one with
the `IdentityUIFrameworkVersion` MSBuild property (`Bootstrap5` or `Tailwind`).


### Shells

`_IdentityLayout.cshtml` frames the page body with a *shell* chosen by `ViewData["Shell"]`:

| Shell | Pages | Structure |
|---|---|---|
| `auth` | Login, Register | Hero aside + form (template-dependent) |
| `card` (default) | Every other anonymous flow, MFA, consent, errors | One centered `.auth-card` |
| `profile` | Profile, Change/Add password, Grants | Image header band, `_ProfileNav` sidebar, `.data-card` stack |
| `article` | Terms, Privacy, Accept terms | Wide `.article-card` with rendered markdown |

### Styling

- Each UI framework has its own stylesheet folder: `wwwroot/css/bootstrap5/` (`bootstrap.bs.scss` +
  `identity.bs.scss` → `bootstrap.bs.css` + `identity.bs.css`) and `wwwroot/css/tailwind/` (`identity.tw.scss` →
  `identity.tw.css`). Nothing is shared between the two trees. The paths below are relative to `wwwroot/css/bootstrap5/`.
- Design tokens live in `abstracts/_tokens.scss` (colours, fluid type scale, spacing, radii, shadows,
  layout widths). They are mapped onto Bootstrap's Sass variables in `bootstrap.bs.scss` and exposed as
  `--idui-*` custom properties for runtime tweaks.
- Custom components use BEM (`.auth-card__title`, `.field__error`, `.provider-btn--microsoft`, …) and live in
  `components/`. Themes live in `themes/` (values only), the layout engine and shells in `layouts/`.
- Stylesheets are compiled by Vite (`npm run build`, also run by `dotnet build`); the compiled
  `wwwroot/css/**/*.css` files are committed.
- Hosts override styles by shipping `wwwroot/css/bootstrap5/identity.bs.css` (replaces the stylesheet) or by adding rules to a
  `wwwroot/css/bootstrap5/_custom.scss` that the package imports last when the SCSS imports feature is enabled.
- The hero image is `wwwroot/img/hero.jpg`; ship a file at the same path to replace it, or set
  `--idui-hero-image` on `:root` / `[data-theme]`.

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
  SCSS sources must use the new entry files (`bootstrap5/*.bs.scss`, `tailwind/identity.tw.scss`).

Hosts that use the package pages and layouts without overriding stylesheets need no changes. The Bootstrap5 variant
was also redesigned in 8.58.0 (new markup and class names); see the CHANGELOG for those breaking changes.

### Custom Page Templates

Override specific pages by creating them in your host application:

1. On the same path as in the library but ommiting the `UIFramework` folder `/Pages/Bootstrap5/Login` will become `/Pages/Login`.
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
│   ├── Bootstrap5/         # Bootstrap 5 theme
│   ├── Tailwind/          # Tailwind CSS theme
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
- **Tag Helpers**: Custom HTML helpers (ProfilePictureImageTagHelper)
- **Event Handlers**: Extensible event system for custom business logic

### Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Indice.Features.Identity.Core | Latest | Core identity functionality |
| FluentValidation.AspNetCore | 11.3.1 | Form validation |
| HtmlAgilityPack | 1.12.3 | HTML parsing and manipulation |
| Bootstrap | 5.3.6 | UI framework |
| jQuery | 3.7.1 | JavaScript functionality |
| Font Awesome | 6.7.2 | Icons |
| Tailwind CSS | 3.4.17 | Utility-first CSS framework |

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
- `/Profile` - User profile management dashboard
- `/ChangePassword` - Password change form
- `/AddEmail` - Add additional email address
- `/AddPhone` - Add phone number
- `/VerifyPhone` - Phone number verification

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
