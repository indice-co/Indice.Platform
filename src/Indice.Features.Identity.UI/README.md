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
    options.HtmlBodyBackgroundCssClass = "gradient-bg";
    
    // Feature Toggles
    options.EnableLocalLogin = true;
    options.AutoProvisionExternalUsers = true;
    options.AutoAssociateExternalUsers = true;
    options.EnablePhoneNumberCallingCodes = false;
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

### Theme Selection

The library supports multiple UI frameworks. Choose your preferred theme by organizing your views:

```
Pages/
├── Bootstrap5/          # Bootstrap 5 theme (default)
│   ├── Login.cshtml
│   ├── Register.cshtml
│   └── ...
├── Tailwind/           # Tailwind CSS theme
│   ├── Login.cshtml
│   ├── Register.cshtml
│   └── ...
```

### Overriding Static Assets

You can override any static asset by placing a file with the same path in your host application's `wwwroot`:

```
wwwroot/
├── css/
│   └── bootstrap.css    # Overrides library's bootstrap.css
├── js/
│   └── app.js       # Additional JavaScript
└── images/
    └── logo.png        # Custom logo
```

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